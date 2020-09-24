using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc2x3.ActorResource;
using Xbim.Ifc2x3.ExternalReferenceResource;
using Xbim.Ifc2x3.GeometricConstraintResource;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.PresentationOrganizationResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.QuantityResource;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.TimeSeriesResource;

namespace HelloWall
{
    class HelloWall
    {
        public static void InitiateModel()

        {
            var editor = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "Christian Dimitri",
                ApplicationFullName = "Hello Wall",
                ApplicationIdentifier = "2020922",
                ApplicationVersion = "4.0",
                //your user
                EditorsFamilyName = "Dimitri",
                EditorsGivenName = "Christian",
                EditorsOrganisationName = "CD"
            };
            using (var model = IfcStore.Create(editor, XbimSchemaVersion.Ifc2X3, Xbim.IO.XbimStoreType.InMemoryModel))
            {
                using (var txn = model.BeginTransaction("Create Building"))
                {
                    var building = model.Instances.New<IfcBuilding>();
                    building.Name = "Default building";
                    building.CompositionType = IfcElementCompositionEnum.ELEMENT;
                    var localPlacement = model.Instances.New<IfcLocalPlacement>();
                    building.ObjectPlacement = localPlacement;
                    var placement = model.Instances.New<IfcAxis2Placement3D>();
                    localPlacement.RelativePlacement = placement;
                    placement.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
                    //get the project there should only be one and it should exist
                    var project = model.Instances.OfType<IfcProject>().FirstOrDefault();
                    project?.AddBuilding(building);
                    txn.Commit();
                };
                // / <summary>
                /// Sets up the basic parameters any model must provide, units, ownership etc
                // / </summary>
                // / <param name="projectName">Name of the project</param>
                // / <returns></returns>
                using (var txn = model.BeginTransaction("Initialise Model"))
                {
                    // there should always be one project in the model
                    var project = model.Instances.New<IfcProject>(p => p.Name = "Basic wall creation");
                    Console.WriteLine(project.Name);
                    // our shortcut to define basic defaults units
                    project.Initialize(ProjectUnits.SIUnitsUK);
                    txn.Commit();
                };
                // / <summary>
                /// This creates a wall and it's geometry, many geometric representations are possible and extruded rectangular footprint is chosen as this is commonly used for standard case walls
                // / </summary>
                // / <param name="model"></param>
                // / <param name="length">Length of the rectangular footprint</param>
                // / <param name="width">Width of the rectangular footprint (width of the wall)</param>
                // / <param name="height">Height to extrude the wall, extrusion is vertical</param>
                // / <returns></returns>

                //
                //begin a transaction
                using (var txn = model.BeginTransaction("Create Wall"))
                {
                    var wall = model.Instances.New<IfcWallStandardCase>();
                    wall.Name = "A Standard rectangular wall";

                    //represent wall as a rectangular profile
                    var rectProf = model.Instances.New<IfcRectangleProfileDef>();
                    rectProf.ProfileType = IfcProfileTypeEnum.AREA;
                    rectProf.XDim = 300;
                    rectProf.YDim = 4000;

                    var insertPoint = model.Instances.New<IfcCartesianPoint>();
                    insertPoint.SetXY(0, 400); //insert at arbitrary position
                    rectProf.Position = model.Instances.New<IfcAxis2Placement2D>();
                    rectProf.Position.Location = insertPoint;

                    //model as a swept area solid
                    var body = model.Instances.New<IfcExtrudedAreaSolid>();
                    body.Depth = 2400;
                    body.SweptArea = rectProf;
                    body.ExtrudedDirection = model.Instances.New<IfcDirection>();
                    body.ExtrudedDirection.SetXYZ(0, 0, 1);

                    //parameters to insert the geometry in the model
                    var origin = model.Instances.New<IfcCartesianPoint>();
                    origin.SetXYZ(0, 0, 0);
                    body.Position = model.Instances.New<IfcAxis2Placement3D>();
                    body.Position.Location = origin;

                    //Create a Definition shape to hold the geometry
                    var shape = model.Instances.New<IfcShapeRepresentation>();
                    var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                    shape.ContextOfItems = modelContext;
                    shape.RepresentationType = "SweptSolid";
                    shape.RepresentationIdentifier = "Body";
                    shape.Items.Add(body);

                    //Create a Product Definition and add the model geometry to the wall
                    var rep = model.Instances.New<IfcProductDefinitionShape>();
                    rep.Representations.Add(shape);
                    wall.Representation = rep;

                    //now place the wall into the model
                    var lp = model.Instances.New<IfcLocalPlacement>();
                    var ax3D = model.Instances.New<IfcAxis2Placement3D>();
                    ax3D.Location = origin;
                    ax3D.RefDirection = model.Instances.New<IfcDirection>();
                    ax3D.RefDirection.SetXYZ(0, 1, 0);
                    ax3D.Axis = model.Instances.New<IfcDirection>();
                    ax3D.Axis.SetXYZ(0, 0, 1);
                    lp.RelativePlacement = ax3D;
                    wall.ObjectPlacement = lp;


                    // Where Clause: The IfcWallStandard relies on the provision of an IfcMaterialLayerSetUsage 
                    var ifcMaterialLayerSetUsage = model.Instances.New<IfcMaterialLayerSetUsage>();
                    var ifcMaterialLayerSet = model.Instances.New<IfcMaterialLayerSet>();
                    var ifcMaterialLayer = model.Instances.New<IfcMaterialLayer>();
                    ifcMaterialLayer.LayerThickness = 10;
                    ifcMaterialLayerSet.MaterialLayers.Add(ifcMaterialLayer);
                    ifcMaterialLayerSetUsage.ForLayerSet = ifcMaterialLayerSet;
                    ifcMaterialLayerSetUsage.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS2;
                    ifcMaterialLayerSetUsage.DirectionSense = IfcDirectionSenseEnum.NEGATIVE;
                    ifcMaterialLayerSetUsage.OffsetFromReferenceLine = 150;

                    // Add material to wall
                    var material = model.Instances.New<IfcMaterial>();
                    material.Name = "CMU_END1";
                    var ifcRelAssociatesMaterial = model.Instances.New<IfcRelAssociatesMaterial>();
                    ifcRelAssociatesMaterial.RelatingMaterial = material;
                    ifcRelAssociatesMaterial.RelatedObjects.Add(wall);

                    ifcRelAssociatesMaterial.RelatingMaterial = ifcMaterialLayerSetUsage;

                    // IfcPresentationLayerAssignment is required for CAD presentation in IfcWall or IfcWallStandardCase
                    var ifcPresentationLayerAssignment = model.Instances.New<IfcPresentationLayerAssignment>();
                    ifcPresentationLayerAssignment.Name = "some ifcPresentationLayerAssignment";
                    ifcPresentationLayerAssignment.AssignedItems.Add(shape);


                    // linear segment as IfcPolyline with two points is required for IfcWall
                    var ifcPolyline = model.Instances.New<IfcPolyline>();
                    var startPoint = model.Instances.New<IfcCartesianPoint>();
                    startPoint.SetXY(0, 0);
                    var endPoint = model.Instances.New<IfcCartesianPoint>();
                    endPoint.SetXY(4000, 0);
                    ifcPolyline.Points.Add(startPoint);
                    ifcPolyline.Points.Add(endPoint);

                    var shape2D = model.Instances.New<IfcShapeRepresentation>();
                    shape2D.ContextOfItems = modelContext;
                    shape2D.RepresentationIdentifier = "Axis";
                    shape2D.RepresentationType = "Curve2D";
                    shape2D.Items.Add(ifcPolyline);
                    rep.Representations.Add(shape2D);
                    txn.Commit();
                    // / <summary>
                    /// Add some properties to the wall,
                    // / </summary>
                    // / <param name="model">XbimModel</param>
                    // / <param name="wall"></param>
                    AddPropertyToWall(model, wall);
                }

                //save your changed model. IfcStore will use the extension to save it as *.ifc, *.ifczip or *.ifcxml.
                model.SaveAs("HelloWall.ifc");
            }
        }
        static private void AddPropertyToWall(IfcStore model, IfcWallStandardCase wall)
        {
            using (var txn = model.BeginTransaction("Create Wall"))
            {
                CreateElementQuantity(model, wall);
                CreateSimpleProperty(model, wall);
                txn.Commit();
            }
        }
        private static void CreateSimpleProperty(IfcStore model, IfcWallStandardCase wall)
        {
            var ifcPropertySingleValue = model.Instances.New<IfcPropertySingleValue>(psv =>
            {
                psv.Name = "IfcPropertySingleValue:Time";
                psv.Description = "";
                psv.NominalValue = new IfcTimeMeasure(150.0);
                psv.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.TIMEUNIT;
                    siu.Name = IfcSIUnitName.SECOND;
                });
            });
            var ifcPropertyEnumeratedValue = model.Instances.New<IfcPropertyEnumeratedValue>(pev =>
            {
                pev.Name = "IfcPropertyEnumeratedValue:Music";
                pev.EnumerationReference = model.Instances.New<IfcPropertyEnumeration>(pe =>
                    {
                        pe.Name = "Notes";
                        pe.EnumerationValues.Add(new IfcLabel("Do"));
                        pe.EnumerationValues.Add(new IfcLabel("Re"));
                        pe.EnumerationValues.Add(new IfcLabel("Mi"));
                        pe.EnumerationValues.Add(new IfcLabel("Fa"));
                        pe.EnumerationValues.Add(new IfcLabel("So"));
                        pe.EnumerationValues.Add(new IfcLabel("La"));
                        pe.EnumerationValues.Add(new IfcLabel("Ti"));
                    });
                pev.EnumerationValues.Add(new IfcLabel("Do"));
                pev.EnumerationValues.Add(new IfcLabel("Re"));
                pev.EnumerationValues.Add(new IfcLabel("Mi"));

            });
            var ifcPropertyBoundedValue = model.Instances.New<IfcPropertyBoundedValue>(pbv =>
            {
                pbv.Name = "IfcPropertyBoundedValue:Mass";
                pbv.Description = "";
                pbv.UpperBoundValue = new IfcMassMeasure(5000.0);
                pbv.LowerBoundValue = new IfcMassMeasure(1000.0);
                pbv.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.MASSUNIT;
                    siu.Name = IfcSIUnitName.GRAM;
                    siu.Prefix = IfcSIPrefix.KILO;
                });
            });

            var definingValues = new List<IfcReal> { new IfcReal(100.0), new IfcReal(200.0), new IfcReal(400.0), new IfcReal(800.0), new IfcReal(1600.0), new IfcReal(3200.0), };
            var definedValues = new List<IfcReal> { new IfcReal(20.0), new IfcReal(42.0), new IfcReal(46.0), new IfcReal(56.0), new IfcReal(60.0), new IfcReal(65.0), };
            var ifcPropertyTableValue = model.Instances.New<IfcPropertyTableValue>(ptv =>
            {
                ptv.Name = "IfcPropertyTableValue:Sound";
                foreach (var item in definingValues)
                {
                    ptv.DefiningValues.Add(item);
                }
                foreach (var item in definedValues)
                {
                    ptv.DefinedValues.Add(item);
                }
                ptv.DefinedUnit = model.Instances.New<IfcContextDependentUnit>(cd =>
                {
                    cd.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                    {
                        de.LengthExponent = 0;
                        de.MassExponent = 0;
                        de.TimeExponent = 0;
                        de.ElectricCurrentExponent = 0;
                        de.ThermodynamicTemperatureExponent = 0;
                        de.AmountOfSubstanceExponent = 0;
                        de.LuminousIntensityExponent = 0;
                    });
                    cd.UnitType = IfcUnitEnum.FREQUENCYUNIT;
                    cd.Name = "dB";
                });


            });

            var listValues = new List<IfcLabel> { new IfcLabel("Red"), new IfcLabel("Green"), new IfcLabel("Blue"), new IfcLabel("Pink"), new IfcLabel("White"), new IfcLabel("Black"), };
            var ifcPropertyListValue = model.Instances.New<IfcPropertyListValue>(plv =>
            {
                plv.Name = "IfcPropertyListValue:Colours";
                foreach (var item in listValues)
                {
                    plv.ListValues.Add(item);
                }
            });

            var ifcMaterial = model.Instances.New<IfcMaterial>(m =>
            {
                m.Name = "Brick";
            });
            var ifcPrValueMaterial = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Material";
                prv.PropertyReference = ifcMaterial;
            });


            var ifcMaterialList = model.Instances.New<IfcMaterialList>(ml =>
                {
                    ml.Materials.Add(ifcMaterial);
                    ml.Materials.Add(model.Instances.New<IfcMaterial>(m => { m.Name = "Cavity"; }));
                    ml.Materials.Add(model.Instances.New<IfcMaterial>(m => { m.Name = "Block"; }));
                });


            var ifcMaterialLayer = model.Instances.New<IfcMaterialLayer>(ml =>
            {
                ml.Material = ifcMaterial;
                ml.LayerThickness = 100.0;
            });
            var ifcPrValueMatLayer = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:MaterialLayer";
                prv.PropertyReference = ifcMaterialLayer;
            });

            var ifcDocumentReference = model.Instances.New<IfcDocumentReference>(dr =>
            {
                dr.Name = "Document";
                dr.Location = "c://Documents//TheDoc.Txt";
            });
            var ifcPrValueRef = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Document";
                prv.PropertyReference = ifcDocumentReference;
            });

            var ifcTimeSeries = model.Instances.New<IfcRegularTimeSeries>(ts =>
            {
                ts.Name = "Regular Time Series";
                ts.Description = "Time series of events";
                ts.TimeSeriesDataType = IfcTimeSeriesDataTypeEnum.CONTINUOUS;
                ts.DataOrigin = IfcDataOriginEnum.MEASURED;
                ts.TimeStep = 604800; //7 days in secs
            });

            var ifcPrValueTimeSeries = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:TimeSeries";
                prv.PropertyReference = ifcTimeSeries;
            });

            var ifcAddress = model.Instances.New<IfcPostalAddress>(a =>
            {
                a.InternalLocation = "Room 101";
                a.AddressLines.AddRange(new[] { new IfcLabel("12 New road"), new IfcLabel("DoxField") });
                a.Town = "Sunderland";
                a.PostalCode = "DL01 6SX";
            });
            var ifcPrValueAddress = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Address";
                prv.PropertyReference = ifcAddress;
            });
            var ifcTelecomAddress = model.Instances.New<IfcTelecomAddress>(a =>
            {
                a.TelephoneNumbers.Add(new IfcLabel("01325 6589965"));
                a.ElectronicMailAddresses.Add(new IfcLabel("bob@bobsworks.com"));
            });
            var ifcPrValueTelecom = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Telecom";
                prv.PropertyReference = ifcTelecomAddress;
            });



            //lets create the IfcElementQuantity
            var ifcPropertySet = model.Instances.New<IfcPropertySet>(ps =>
            {
                ps.Name = "Test:IfcPropertySet";
                ps.Description = "Property Set";
                ps.HasProperties.Add(ifcPropertySingleValue);
                ps.HasProperties.Add(ifcPropertyEnumeratedValue);
                ps.HasProperties.Add(ifcPropertyBoundedValue);
                ps.HasProperties.Add(ifcPropertyTableValue);
                ps.HasProperties.Add(ifcPropertyListValue);
                ps.HasProperties.Add(ifcPrValueMaterial);
                ps.HasProperties.Add(ifcPrValueMatLayer);
                ps.HasProperties.Add(ifcPrValueRef);
                ps.HasProperties.Add(ifcPrValueTimeSeries);
                ps.HasProperties.Add(ifcPrValueAddress);
                ps.HasProperties.Add(ifcPrValueTelecom);
            });

            //need to create the relationship
            model.Instances.New<IfcRelDefinesByProperties>(rdbp =>
            {
                rdbp.Name = "Property Association";
                rdbp.Description = "IfcPropertySet associated to wall";
                rdbp.RelatedObjects.Add(wall);
                rdbp.RelatingPropertyDefinition = ifcPropertySet;
            });
        }

        private static void CreateElementQuantity(IfcStore model, IfcWallStandardCase wall)
        {
            //Create a IfcElementQuantity
            //first we need a IfcPhysicalSimpleQuantity,first will use IfcQuantityLength
            var ifcQuantityArea = model.Instances.New<IfcQuantityLength>(qa =>
            {
                qa.Name = "IfcQuantityArea:Area";
                qa.Description = "";
                qa.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.LENGTHUNIT;
                    siu.Prefix = IfcSIPrefix.MILLI;
                    siu.Name = IfcSIUnitName.METRE;
                });
                qa.LengthValue = 100.0;

            });
            //next quantity IfcQuantityCount using IfcContextDependentUnit
            var ifcContextDependentUnit = model.Instances.New<IfcContextDependentUnit>(cd =>
                {
                    cd.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                        {
                            de.LengthExponent = 1;
                            de.MassExponent = 0;
                            de.TimeExponent = 0;
                            de.ElectricCurrentExponent = 0;
                            de.ThermodynamicTemperatureExponent = 0;
                            de.AmountOfSubstanceExponent = 0;
                            de.LuminousIntensityExponent = 0;
                        });
                    cd.UnitType = IfcUnitEnum.LENGTHUNIT;
                    cd.Name = "Elephants";
                });
            var ifcQuantityCount = model.Instances.New<IfcQuantityCount>(qc =>
            {
                qc.Name = "IfcQuantityCount:Elephant";
                qc.CountValue = 12;
                qc.Unit = ifcContextDependentUnit;
            });


            //next quantity IfcQuantityLength using IfcConversionBasedUnit
            var ifcConversionBasedUnit = model.Instances.New<IfcConversionBasedUnit>(cbu =>
            {
                cbu.ConversionFactor = model.Instances.New<IfcMeasureWithUnit>(mu =>
                {
                    mu.ValueComponent = new IfcRatioMeasure(25.4);
                    mu.UnitComponent = model.Instances.New<IfcSIUnit>(siu =>
                    {
                        siu.UnitType = IfcUnitEnum.LENGTHUNIT;
                        siu.Prefix = IfcSIPrefix.MILLI;
                        siu.Name = IfcSIUnitName.METRE;
                    });

                });
                cbu.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                {
                    de.LengthExponent = 1;
                    de.MassExponent = 0;
                    de.TimeExponent = 0;
                    de.ElectricCurrentExponent = 0;
                    de.ThermodynamicTemperatureExponent = 0;
                    de.AmountOfSubstanceExponent = 0;
                    de.LuminousIntensityExponent = 0;
                });
                cbu.UnitType = IfcUnitEnum.LENGTHUNIT;
                cbu.Name = "Inch";
            });
            var ifcQuantityLength = model.Instances.New<IfcQuantityLength>(qa =>
            {
                qa.Name = "IfcQuantityLength:Length";
                qa.Description = "";
                qa.Unit = ifcConversionBasedUnit;
                qa.LengthValue = 24.0;
            });

            //lets create the IfcElementQuantity
            var ifcElementQuantity = model.Instances.New<IfcElementQuantity>(eq =>
            {
                eq.Name = "Test:IfcElementQuantity";
                eq.Description = "Measurement quantity";
                eq.Quantities.Add(ifcQuantityArea);
                eq.Quantities.Add(ifcQuantityCount);
                eq.Quantities.Add(ifcQuantityLength);
            });

            //need to create the relationship
            model.Instances.New<IfcRelDefinesByProperties>(rdbp =>
            {
                rdbp.Name = "Area Association";
                rdbp.Description = "IfcElementQuantity associated to wall";
                rdbp.RelatedObjects.Add(wall);
                rdbp.RelatingPropertyDefinition = ifcElementQuantity;
            });
        }
    }
}