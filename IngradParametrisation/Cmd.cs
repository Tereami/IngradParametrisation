#region License
/*Данный код опубликован под лицензией Creative Commons Attribution-NonСommercial-ShareAlike.
Разрешено использовать, распространять, изменять и брать данный код за основу для производных 
в некоммерческих целях, при условии указания авторства и если производные лицензируются на тех же условиях.
Код поставляется "как есть". Автор не несет ответственности за возможные последствия использования.
Зуев Александр, 2021, все права защищены.
This code is listed under the Creative Commons Attribution-NonСommercial-ShareAlike license.
You may use, redistribute, remix, tweak, and build upon this work non-commercially,
as long as you credit the author by linking back and license your new creations under the same terms.
This code is provided 'as is'. Author disclaims any implied warranty.
Zuev Aleksandr, 2021, all rigths reserved.*/
#endregion
#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace IngradParametrisation
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class Cmd : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Debug.Listeners.Clear();
            Debug.Listeners.Add(new RbsLogger.Logger("IngdParametrisation"));

            string paramNameGroupConstr = "INGD_Группа конструкции";
            string floorNumberParamName = "INGD_Номер этажа";
            int floorTextPosition = 0;

            App.assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            string markParamName = "INGD_Марка";
            string widthParamName = "INGD_Ширина";
            string lengthParamName = "INGD_Длина";
            string heigthParamName = "INGD_Высота";

            Dictionary<string, string> marksBase = new Dictionary<string, string>();

            string txtFile = SettingsUtils.CheckOrCreateSettings();

            if(!System.IO.File.Exists(txtFile))
            {
                message = "Не найден файл " + txtFile;
                return Result.Failed;
            }

            string[] data = System.IO.File.ReadAllLines(txtFile);
            foreach(string s in data)
            {
                string[] line = s.Split(';');
                marksBase.Add(line[0], line[1]);
            }
            Debug.WriteLine("Marks found: " + marksBase.Count.ToString());

            Document doc = commandData.Application.ActiveUIDocument.Document;

            List<Element> allElems = new List<Element>();

            List<Wall> walls = new FilteredElementCollector(doc)
                .OfClass(typeof(Wall))
                .Cast<Wall>()
                .ToList();

            List<Floor> floors = new FilteredElementCollector(doc)
                .OfClass(typeof(Floor))
                .Cast<Floor>()
                .ToList();

            allElems.AddRange(walls);
            allElems.AddRange(floors);
            allElems.AddRange(new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Architecture.Stairs)));
            allElems.AddRange(new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralColumns));
            allElems.AddRange(new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralFraming));
            allElems.AddRange(new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralFoundation));
            allElems.AddRange(new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_Floors));
            allElems.AddRange(new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_Walls));

            List<FamilyInstance> genericModels = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .Cast<FamilyInstance>()
                .Where(i => !i.Symbol.FamilyName.StartsWith("22"))
                .ToList();
            allElems.AddRange(genericModels);

            List<FamilyInstance> windows = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(i => i.Symbol.FamilyName.StartsWith("23"))
                .ToList();
            allElems.AddRange(windows);

            List<RoofBase> roofs = new FilteredElementCollector(doc)
                .OfClass(typeof(Autodesk.Revit.DB.RoofBase))
                .Cast<RoofBase>()
                .ToList();

            int count = 0;
            Debug.WriteLine("Elems found: " + allElems.Count.ToString());

            using (Transaction t = new Transaction(doc))
            {
                t.Start("INGD Параметризация");

                foreach (Element elem in allElems)
                {
                    Debug.WriteLine("Element id: " + elem.Id.IntegerValue.ToString());
                    Parameter markParam = elem.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                    if (markParam == null) continue;
                    if (markParam.HasValue)
                    {
                        string mark = markParam.AsString();
                        Debug.WriteLine("Mark = " + mark);

                        Parameter ingdMarkParam = elem.LookupParameter(markParamName);
                        
                        if (ingdMarkParam != null)
                        {
                            ingdMarkParam.Set(mark);
                        }

                        string[] splitmark = mark.Split('-');
                        if (splitmark.Length > 1)
                        {
                            string markPrefix = splitmark[0];
                            if (!marksBase.ContainsKey(markPrefix))
                            {
                                message = "Недопустимый префикс марки " + markPrefix + " у элемента id " + elem.Id.IntegerValue.ToString();
                                Debug.WriteLine(message);
                                return Result.Failed;
                            }
                            string group = marksBase[markPrefix];
                            Debug.WriteLine("Group name: " + group);
                            Parameter groupParam = elem.LookupParameter(paramNameGroupConstr);
                            if (groupParam != null)
                            {
                                groupParam.Set(group);
                            }
                        }
                    }

                    //заполняю номер этажа
                    Level baseLevel = LevelUtils.GetLevelOfElement(elem, doc);
                    if (baseLevel != null)
                    {
                        string floorNumber = LevelUtils.GetFloorNumberByLevel(baseLevel, floorTextPosition);
                        Parameter ingdFloor = elem.LookupParameter(floorNumberParamName);
                        if (ingdFloor == null)
                        {
                            Debug.WriteLine("No parameter: " + floorNumberParamName);
                            continue;
                        }
                        ingdFloor.Set(floorNumber);
                    }

                    count++;
                }

                foreach (RoofBase roof in roofs)
                {
                    Debug.WriteLine("Roof id: " + roof.Id.IntegerValue.ToString());
                    string group = "Бетонная подготовка";
                    Parameter groupParam = roof.LookupParameter(paramNameGroupConstr);
                    if (groupParam != null)
                    {
                        groupParam.Set(group);
                    }
                    else
                    {
                        Debug.WriteLine("No parameter: " + paramNameGroupConstr);
                    }

                }

                foreach (Wall w in walls)
                {
                    Debug.WriteLine("Wall id: " + w.Id.IntegerValue.ToString());
                    Level baseLevel = doc.GetElement(w.LevelId) as Level;
                    string floorNumber = LevelUtils.GetFloorNumberByLevel(baseLevel, floorTextPosition);
                    Parameter ingdFloor = w.LookupParameter(floorNumberParamName);
                    if (ingdFloor == null)
                    {
                        Debug.WriteLine("No parameter: " + floorNumberParamName);
                        continue;
                    }
                    ingdFloor.Set(floorNumber);

                    double width = w.Width;
                    Parameter ingdWidth = w.LookupParameter(widthParamName);
                    if (ingdWidth == null)
                    {
                        Debug.WriteLine("No parameter: " + widthParamName);
                        continue;
                    }
                    ingdWidth.Set(width);

                    double length = w.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
                    Parameter ingdLength = w.LookupParameter(lengthParamName);
                    if (ingdLength == null)
                    {
                        Debug.WriteLine("No parameter: " + lengthParamName);
                        continue;
                    }
                    ingdLength.Set(length);

                    List<Solid> solids = GeometryUtils.GetSolidsFromElement(w);
                    XYZ[] maxminZ = GeometryUtils.GetMaxMinHeightPoints(solids);
                    double heigth = maxminZ[0].Z - maxminZ[1].Z;
                    Parameter ingdHeigth = w.LookupParameter(heigthParamName);
                    if (ingdHeigth == null)
                    {
                        Debug.WriteLine("No parameter: " + heigthParamName);
                        continue;
                    }
                    ingdHeigth.Set(heigth);
                }

                foreach (Floor f in floors)
                {
                    Debug.WriteLine("Floor id: " + f.Id.IntegerValue.ToString());
                    Level baseLevel = doc.GetElement(f.LevelId) as Level;
                    string floorNumber = LevelUtils.GetFloorNumberByLevel(baseLevel, floorTextPosition);
                    Parameter ingdFloor = f.LookupParameter(floorNumberParamName);
                    if (ingdFloor == null)
                    {
                        Debug.WriteLine("No parameter: " + floorNumberParamName);
                        continue;
                    }
                    ingdFloor.Set(floorNumber);

                    FloorType ft = f.FloorType;

                    double heigth = ft.get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM).AsDouble();
                    Parameter ingdHeigth = f.LookupParameter(heigthParamName);
                    if (ingdHeigth == null)
                    {
                        Debug.WriteLine("No parameter: " + heigthParamName);
                        continue;
                    }
                    ingdHeigth.Set(heigth);
                }

                t.Commit();
            }

            Debug.WriteLine("Elements are done: " + count.ToString());
            TaskDialog.Show("Отчет", "Обработано элементов: " + count.ToString());
            return Result.Succeeded;
        }
    }
}
