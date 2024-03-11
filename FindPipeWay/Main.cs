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

        public bool IsThreeWay (Document doc, ElementId elementId)
        {
            
            Element element = doc.GetElement(elementId);
            bool isthreeway = false;
            if (element is FamilyInstance)
            {
                if (element is FamilyInstance)
                {
                    FamilyInstance fI = element as FamilyInstance;
                    MEPModel mepMoidel = fI.MEPModel;
                    ConnectorSet connectorSet = mepMoidel.ConnectorManager.Connectors;
                    List<Connector> connectors = new List<Connector>();
                    foreach (Connector connector in connectorSet)
                    {
                        connectors.Add(connector);
                    }
                    if (connectors.Count>=3)
                    {
                        isthreeway = true;
                    }
                    elementId = element.Id;
                }
                if (element is Pipe)
                {
                    Pipe fI = element as Pipe;
                    
                    ConnectorSet connectorSet = fI.ConnectorManager.Connectors;
                    List<Connector> connectors = new List<Connector>();
                    foreach (Connector connector in connectorSet)
                    {
                        connectors.Add(connector);
                    }
                    if (connectors.Count >=3)
                    {
                        isthreeway = true;
                    }
                    elementId = element.Id;
                }
            }
            return isthreeway;
        }

       
        public ElementId GetDirection (Document doc, ElementId elementId )
        {
            ElementId selectedpipe = null;
            List <ElementId> pipes = new List<ElementId>();
            
                Element element = doc.GetElement(elementId);
                if (element!=null)
                {
                    FamilyInstance familyInstance = element as FamilyInstance;
                    MEPModel mepmodel = familyInstance.MEPModel;
                    ConnectorSet connectorSet = mepmodel.ConnectorManager.Connectors;
                    foreach (Connector connector in connectorSet)
                    {
                        ConnectorSet nextconnectorset = connector.AllRefs;
                        foreach (Connector nextconnector in nextconnectorset)
                        {
                            ElementId nextelementId = nextconnector.Owner.Id;
                            pipes.Add(nextelementId);
                            
                        }
                    }
                }
            
            double maxVolume = 0;
            foreach (ElementId pipe in pipes)
            {
                
                Element el = doc.GetElement(pipe);
                Pipe pipe1 = el as Pipe;
                if (pipe1 != null)
                {
                   foreach ( Parameter parameter in pipe1.Parameters )
                    {
                        if (parameter.Definition.Name == "Расход")
                        {
                            double volume = parameter.AsDouble();
                            if (volume > maxVolume)
                            {
                                maxVolume = volume;
                                selectedpipe = pipe1.Id;
                            }
                        }

                    }
                    
                }
            }
            return selectedpipe;
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
                    if (IsThreeWay(doc, nextelement) == true)
                    {

                        var nf = GetDirection(doc, nextelement);
                        if (nf != null)
                        {
                            if (!foundedelements.Contains(nf))
                            {
                                foundedelements.Add(nf);
                                f = FindNextElement(doc, nf, foundedelements);
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
                            else
                            {
                                continue;
                            }


                        }
                    }
                    else
                    {

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
