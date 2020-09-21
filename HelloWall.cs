using System;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc2x3.Interfaces;
namespace HelloWall
{
    class HelloWall
    {
        public static void InitiateModel()
        {
            Console.WriteLine("Hello Wall");
            const string fileName = "SampleHouse.ifc"; //this can be either IFC2x3 or IFC4
            var editor = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "You",
                ApplicationFullName = "Your app",
                ApplicationIdentifier = "Your app ID",
                ApplicationVersion = "4.0",
                //your user
                EditorsFamilyName = "Santini Aichel",
                EditorsGivenName = "Johann Blasius",
                EditorsOrganisationName = "Independent Architecture"
            };
            using (var model = IfcStore.Open(fileName, editor))
            {
                using (var txn = model.BeginTransaction("Quick start transaction"))
                {
                    //get all walls in the model
                    var walls = model.Instances.OfType<IIfcWall>();

                    //iterate over all the walls and change them
                    foreach (var wall in walls)
                    {
                        wall.Name = "Iterated wall: " + wall.Name;
                    }

                    //commit your changes
                    txn.Commit();
                }

                //save your changed model. IfcStore will use the extension to save it as *.ifc, *.ifczip or *.ifcxml.
                model.SaveAs("SampleHouse_Modified.ifc");
            }
        }
    }
}