using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;

namespace FindPipeWay
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Element  FindNextElement (Connector connector)
        {
            ElementId ownerId = connector.Owner.Id;
            Element foundedElement = null;
            ConnectorSet  connectorSet= connector.AllRefs;
            foreach (Connector connector_self in connectorSet)
            {
                if (connector_self.Owner.Id!= ownerId && connector_self.Owner.Category.Name!="Трубопроводные системы")
                {
                    foundedElement = connector_self.Owner;
                       
                }
            }
            TaskDialog.Show("E", foundedElement.Id.ToString());
            return foundedElement;
        }
        public ElementId FindNextElement (Document doc, ElementId elementId,List<ElementId> foundedelements)
        {
            
                ElementId ownerId = elementId;
                Element element = doc.GetElement(ownerId);
                Element foundedElement = null;
                MEPModel mepModel = null;
                ConnectorSet connectorSet = null;
                ElementId foundedelementId = null;
            try
            {
                if (element is FamilyInstance)
                {
                    FamilyInstance FI = element as FamilyInstance;
                    mepModel = FI.MEPModel;
                    connectorSet = mepModel.ConnectorManager.Connectors;
                }
                if (element is Pipe)
                {
                    Pipe pipe = element as Pipe;
                    connectorSet = pipe.ConnectorManager.Connectors;
                }
                if (element.Category.Name == "Трубопроводные системы")
                {
                    return null;
                }

                
            }
            catch
            {
                TaskDialog.Show("Error", $"{element.Id} не отработал");
                
            }
            foreach (Connector connector in connectorSet)
            {
                ConnectorSet nextconnectors = connector.AllRefs;
                foreach (Connector nextconnector in nextconnectors)
                {
                    if (nextconnector.IsConnected == true)
                    {
                        if (nextconnector.Owner.Id != ownerId)
                        {
                            if (foundedelements.Contains(nextconnector.Owner.Id))
                            {
                                continue;
                            }
                            else
                            {
                                foundedElement = nextconnector.Owner;
                                 foundedelementId = foundedElement.Id;
                            }
                            
                            //TaskDialog.Show("Check", $"{element.Id}, {foundedElement.Id}");
                        }
                    }
                    else
                    {
                        break;
                    }







                }
            }
            return foundedelementId;

            
            
            
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            var pipesystems = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_PipingSystem).WhereElementIsNotElementType().ToElements();
            
            string  pipesystem = pipesystems.FirstOrDefault().Name;
            
            var mepequipments = new FilteredElementCollector ( doc).OfCategory(BuiltInCategory.OST_MechanicalEquipment).WhereElementIsNotElementType().ToElements();
            Dictionary<Connector, double> selectedEquipments = new Dictionary<Connector, double>();
            foreach ( var mepequipment in mepequipments)
            {
               
                if (mepequipment.LookupParameter("Имя системы").AsString().Equals( pipesystem)==true)
                {
                    FamilyInstance FI = mepequipment as FamilyInstance;
                    MEPModel mepmodel = FI.MEPModel;
                    ConnectorSet connectorSet = mepmodel.ConnectorManager.Connectors;
                    foreach ( Connector connector in connectorSet)
                    {
                       
                        if (connector.MEPSystem!=null)
                        {
                            var flow = connector.Flow;
                            selectedEquipments.Add(connector, flow);
                        }
                    }
                    
                }
            }
            var startconnector = selectedEquipments.Aggregate((x,y)=>x.Value>y.Value ? x : y).Key;
            var maxvalue = selectedEquipments.Aggregate((x, y) => x.Value > y.Value ? x : y).Value;
            List<ElementId> foundedelements = new List<ElementId>();

            foundedelements.Add(startconnector.Owner.Id);
            var foundedelement = FindNextElement(startconnector);
            foundedelements.Add(foundedelement.Id);
           
            
            
            
            int index = foundedelements.Count - 1;
            ElementId nextelement = null;
            
            ElementId f = null;
            string name = "";
            try
            {
                do
                {

                    nextelement = foundedelements.Last();
                    foundedelements.Distinct();
                    
                    f = FindNextElement(doc, nextelement, foundedelements);
                    if (f != null)
                    {
                        if (!foundedelements.Contains(f))
                        {
                            if (f != nextelement)
                            {
                                foundedelements.Add(f);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        


                    }
                    else
                    {

                        continue;
                    }

                }
                while (f != nextelement);
            }
            catch
            {
              
                
            }
                
            
           
           
            string text = "";
            
            foreach (var foundedelement2 in foundedelements)
            {
                string a = $"{foundedelement2}  \n";
                text += a;
            }
            TaskDialog.Show("R", text);
            return Result.Succeeded;









        }
    }
}
