﻿#region License
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
using System.Diagnostics;
using System.Collections.Generic;
using Autodesk.Revit.DB;
#endregion

namespace IngradParametrisation
{
    public static class GeometryUtils
    {
        public static XYZ[] GetMaxMinHeightPoints(List<Solid> solids)
        {
            XYZ maxZpoint = new XYZ(0, 0, -9999999);
            XYZ minZpount = new XYZ(0, 0, 9999999);

            List<Edge> edges = new List<Edge>();
            foreach (Solid s in solids)
            {
                foreach (Edge e in s.Edges)
                {
                    edges.Add(e);
                }
            }

            foreach (Edge e in edges)
            {
                Curve c = e.AsCurve();
                XYZ p1 = c.GetEndPoint(0);
                if (p1.Z > maxZpoint.Z) maxZpoint = p1;
                if (p1.Z < minZpount.Z) minZpount = p1;

                XYZ p2 = c.GetEndPoint(1);
                if (p2.Z > maxZpoint.Z) maxZpoint = p2;
                if (p2.Z < minZpount.Z) minZpount = p2;
            }
            XYZ[] result = new XYZ[] { maxZpoint, minZpount };
            return result;
        }


        public static List<Solid> GetSolidsFromElement(Element elem)
        {
            Options opt = new Options();
            opt.ComputeReferences = true;
            opt.DetailLevel = ViewDetailLevel.Fine;
            GeometryElement geoElem = elem.get_Geometry(opt);

            List<Solid> solids = GetSolidsFromElement(geoElem);
            return solids;
        }

        public static List<Solid> GetSolidsFromElement(GeometryElement geoElem)
        {
            Trace.WriteLine("Get solids from geoelem");
            List<Solid> solids = new List<Solid>();

            foreach (GeometryObject geoObj in geoElem)
            {
                if (geoObj is Solid)
                {
                    Solid solid = geoObj as Solid;
                    if (solid == null) continue;
                    if (solid.Volume == 0) continue;
                    solids.Add(solid);
                    continue;
                }
                if (geoObj is GeometryInstance)
                {
                    GeometryInstance geomIns = geoObj as GeometryInstance;
                    GeometryElement instGeoElement = geomIns.GetInstanceGeometry();
                    List<Solid> solids2 = GetSolidsFromElement(instGeoElement);
                    solids.AddRange(solids2);
                }
            }
            Trace.WriteLine("Solids found: " + solids.Count.ToString());
            return solids;
        }

    }
}
