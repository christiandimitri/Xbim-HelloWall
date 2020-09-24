using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    class Wall
    {
        public static void GenerateModel()
        {
            //first create and initialise a model called Hello Wall
            Console.WriteLine("Initialising the IFC Project....");
            using (var model = CreateandInitModel("HelloWall"))
            {
                if (model != null)
                {

                }
                else
                {
                    Console.WriteLine("Failed to initialise the model");
                }

            }
            Console.WriteLine("Press any key to exit to view the IFC file....");
            Console.Read();
            // LaunchNotepad("HelloWallIfc4.ifc");
        }

        private static IfcStore CreateandInitModel(string projectName)
        {
            //first we need to set up some credentials for ownership of data in the new model
            var credentials = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "xbim developer",
                ApplicationFullName = "Hello Wall Application",
                ApplicationIdentifier = "HelloWall.exe",
                ApplicationVersion = "1.0",
                EditorsFamilyName = "Team",
                EditorsGivenName = "xbim",
                EditorsOrganisationName = "xbim developer"
            };
            //now we can create an IfcStore, it is in Ifc4 format and will be held in memory rather than in a database
            //database is normally better in performance terms if the model is large >50MB of Ifc or if robust transactions are required
            var model = IfcStore.Create(credentials, XbimSchemaVersion.Ifc2X3, Xbim.IO.XbimStoreType.InMemoryModel);
            using (model)
            {
                using (var txn = model.BeginTransaction("Initialise Model"))
                {
                    //create a project
                    var project = model.Instances.New<IfcProject>();
                    //set the units to SI (mm and metres)
                    project.Initialize(ProjectUnits.SIUnitsUK);
                    project.Name = projectName;
                    //now commit the changes, else they will be rolled back at the end of the scope of the using statement
                    txn.Commit();
                }
            }
            return model;
        }
    }
}