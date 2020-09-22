using System;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc2x3.GeometricConstraintResource;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.Interfaces;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.PresentationOrganizationResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.Ifc2x3.SharedBldgElements;

namespace HelloWall
{
    class HelloWall
    {
        public static void InitiateModel()
        {
            Console.WriteLine("Hello Wall");
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
                }





                //save your changed model. IfcStore will use the extension to save it as *.ifc, *.ifczip or *.ifcxml.
                model.SaveAs("HelloWall.ifc");
            }
        }
    }
}