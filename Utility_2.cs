using System;
using NXOpen;
using NXOpen.UF;
using NXOpen.Features;
using NXOpen.Utilities;
using System.Collections.Generic;

namespace NX_utilities11
{
    public class Utility_2
    {
        private static Session theSession ;
        private static Part workPart;
        private static Part displayPart;
        private static UFSession theUFSession;
        private static Utility_2 theProgram;

        public Utility_2()
        {
      
            theUFSession = UFSession.GetUFSession();
            theSession = Session.GetSession();
            workPart = theSession.Parts.Work;
            displayPart = theSession.Parts.Display;
 
        }
        /*public static int Main(string[] args)
        {
            int retValue = 0;
            try
            {
                theProgram = new Utility_1();

                //TODO: Add your application code here 

            }
            catch (NXOpen.NXException ex)
            {
                // ---- Enter your exception handling code here -----

            }
            return retValue;
        }*/

        /*asdas a*/
        
        // 得到曲面的中心点
        public static Point3d GetCenterPoint(Face face)
        {
            IntPtr evaluator;
            theUFSession.Evalsf.Initialize2(face.Tag, out evaluator);  //初始化面部评估器结构 
            double[] uv_min_max = new double[4] { 0.0, 1.0, 0.0, 1.0 };
            theUFSession.Evalsf.AskFaceUvMinmax(evaluator, uv_min_max);   //计算面的u，v参数空间min，max 
            double[] uv_pair = { 0.5 * (uv_min_max[0] + uv_min_max[1]), 0.5 * (uv_min_max[2] + uv_min_max[3]) };

            ModlSrfValue surf_eval;
            theUFSession.Evalsf.Evaluate(evaluator, UFConstants.UF_MODL_EVAL_ALL, uv_pair, out surf_eval); //在给定参数值下评估面的点和导数。

            Point3d origin;
            origin.X = surf_eval.srf_pos[0];
            origin.Y = surf_eval.srf_pos[1];
            origin.Z = surf_eval.srf_pos[2];

            return origin;
        }

        //得到中心点
        public static Point3d GetCenterPoint2(Edge edge)
        {
            NXOpen.Section section = workPart.Sections.CreateSection(edge);
            //转化成为arc
            Arc arc = (Arc)workPart.Curves.CreateSmartCompositeCurve(section, SmartObject.UpdateOption.WithinModeling, 0.001);
            Point3d pt = arc.CenterPoint;

            return pt;
        }

        //判断有多少个实体
        private static List<Body> FindAllSolidInLayer(int layer)
        {
            List<Body> result = new List<Body>();
            Tag obj = Tag.Null;
            UFObj.DispProps dispProps;
            int type, subtype, bodytype;

            obj = theUFSession.Obj.CycleAll(workPart.Tag, obj); // 用于遍历UF环境下的所有对象
            while (obj != Tag.Null)
            {
                theUFSession.Obj.AskTypeAndSubtype(obj, out type, out subtype);
                if (type == UFConstants.UF_solid_type && subtype == UFConstants.UF_solid_body_subtype && theUFSession.Obj.AskStatus(obj) == UFConstants.UF_OBJ_ALIVE)
                {
                    theUFSession.Modl.AskBodyType(obj, out bodytype);
                    theUFSession.Obj.AskDisplayProperties(obj,out dispProps);

                    if (bodytype == UFConstants.UF_MODL_SOLID_BODY && dispProps.layer == 1)
                    {
                        Body body = (Body)NXObjectManager.Get(obj);
                        result.Add(body);
                    }
                }
                obj = theUFSession.Obj.CycleAll(workPart.Tag, obj);
            }
            return result;
        }
        //Selection
        public static Body SelectBody(string message, string title)
        {
            TaggedObject body;
            Point3d pos;
            Selection.MaskTriple[] mask_arry = new Selection.MaskTriple[1];
            mask_arry[0].Type = UFConstants.UF_solid_type;
            mask_arry[0].Subtype = UFConstants.UF_solid_body_subtype;
            mask_arry[0].SolidBodySubtype = 0;
            Selection.Response resp = UI.GetUI().SelectionManager.SelectTaggedObject(message, title,
            Selection.SelectionScope.AnyInAssembly,
            Selection.SelectionAction.ClearAndEnableSpecific, false, false, mask_arry, out body, out pos);
            if (resp == Selection.Response.Back || resp == Selection.Response.Cancel)
                return null;
            else return (Body)body;
        }

        //得到所有workpart上的实体
        public static Body[] GetBody()
        {
            BodyCollection bodyCollection = workPart.Bodies;
            Body[] bodies = bodyCollection.ToArray();
            return bodies;
        }

        //拉伸
        public static void Extrude(Curve curve, Double limit)
        {
            //创建一个ExtrudeBuilder对象
            NXOpen.Features.ExtrudeBuilder extrudeBuilder1;
            extrudeBuilder1 = workPart.Features.CreateExtrudeBuilder(null);

            //设置挤出方向和距离
            extrudeBuilder1.Limits.StartExtend.Value.RightHandSide = "0";
            extrudeBuilder1.Limits.EndExtend.Value.RightHandSide = String.Format("{0}", limit);

            NXOpen.Point3d origin4 = new NXOpen.Point3d(0.0, 0.0, 0.0);
            NXOpen.Vector3d vector1 = new NXOpen.Vector3d(0.0, 0.0, 1.0);
            NXOpen.Direction direction2 = workPart.Directions.CreateDirection(origin4, vector1, NXOpen.SmartObject.UpdateOption.WithinModeling);
            extrudeBuilder1.Direction = direction2;

            //设置挤出的特征类型
            extrudeBuilder1.BooleanOperation.Type = NXOpen.GeometricUtilities.BooleanOperation.BooleanType.Create;

            // 设置挤出的剖面为选定曲线
            extrudeBuilder1.Section = workPart.Sections.CreateSection(curve);
            Section section;
            extrudeBuilder1.AllowSelfIntersectingSection(true);
            extrudeBuilder1.ParentFeatureInternal = false;

            NXOpen.Features.Feature feature;
            feature = extrudeBuilder1.CommitFeature();
        }

        public static void ExtrudeCrvs(Curve[] curves, Double limit)
        {
            //创建一个ExtrudeBuilder对象
            NXOpen.Features.ExtrudeBuilder extrudeBuilder1;
            extrudeBuilder1 = workPart.Features.CreateExtrudeBuilder(null);

            //设置挤出方向和距离
            extrudeBuilder1.Limits.StartExtend.Value.RightHandSide = "0";
            extrudeBuilder1.Limits.EndExtend.Value.RightHandSide = String.Format("{0}", limit);

            NXOpen.Point3d origin4 = new NXOpen.Point3d(0.0, 0.0, 0.0);
            NXOpen.Vector3d vector1 = new NXOpen.Vector3d(0.0, 0.0, 1.0);
            NXOpen.Direction direction2 = workPart.Directions.CreateDirection(origin4, vector1, NXOpen.SmartObject.UpdateOption.WithinModeling);
            extrudeBuilder1.Direction = direction2;

            //设置挤出的特征类型
            extrudeBuilder1.BooleanOperation.Type = NXOpen.GeometricUtilities.BooleanOperation.BooleanType.Create;

            // 设置挤出的剖面为选定曲线
            Section section1 = workPart.Sections.CreateSection();

            NXObject nullNXOpen_NXObject = null;
            Curve nullCurve = null;

            CurveChainRule curveChainRule;
            SelectionIntentRule[] rules = new SelectionIntentRule[1];

            Point3d helppoint = new NXOpen.Point3d(0, 0, 0);


            foreach (Curve crv in curves)
            {
                curveChainRule = workPart.ScRuleFactory.CreateRuleCurveChain(crv, nullCurve, false, 0.001);
                rules[0] = curveChainRule;

                section1.AddToSection(rules, crv, nullNXOpen_NXObject, nullNXOpen_NXObject, helppoint, Section.Mode.Create, false);
            }
            extrudeBuilder1.AllowSelfIntersectingSection(true);

            extrudeBuilder1.ParentFeatureInternal = false;
            extrudeBuilder1.Section = section1;
            NXOpen.Features.Feature feature;
            feature = extrudeBuilder1.CommitFeature();
        }

        public static void DeleteObject(NXObject nXObject)
        {
            NXOpen.TaggedObject[] objects1 = new NXOpen.TaggedObject[1];
            objects1[0] = nXObject;
            int nErrs1;
            nErrs1 = theSession.UpdateManager.AddObjectsToDeleteList(objects1);

        }

        public static Curve OffsetCurve(Curve curve, Double set_double)

        {
            Feature nullNXOpen_Feature = null;
            OffsetCurveBuilder offsetCurveBuilder = workPart.Features.CreateOffsetCurveBuilder(nullNXOpen_Feature);

            offsetCurveBuilder.Type = OffsetCurveBuilder.Types.Distance;

            NXOpen.Curve[] curves = new Curve[1];
            curves[0] = curve;
            NXOpen.CurveDumbRule curveDumbRule = workPart.ScRuleFactory.CreateRuleCurveDumb(curves);
            NXOpen.SelectionIntentRule[] rules = new NXOpen.SelectionIntentRule[1];
            rules[0] = curveDumbRule;
            NXObject nullNXobject = null;
            Point3d helpPoint = new Point3d(0, 0, 0);

            offsetCurveBuilder.CurvesToOffset.AddToSection(rules, curve, nullNXobject, nullNXobject, helpPoint, Section.Mode.Create, false);

            offsetCurveBuilder.OffsetDistance.RightHandSide = String.Format("{0}", set_double);

            NXObject nxobject = offsetCurveBuilder.Commit();

            NXOpen.Features.OffsetCurve offsetCurve = (OffsetCurve)nxobject;
            Curve crv = (Curve)offsetCurve.GetEntities()[0];
            offsetCurveBuilder.Destroy();

            return crv;

        }
        public static Double Max(Double[] arr)
        {
            Double max = arr[0];
            for (int i = 1; i < arr.Length; i++)
            {
                if(arr[i] > max)
                {
                    max = arr[i];
                }
            }
            return max;
        }
        public static Double Min(Double[] arr)
        {
            Double min = arr[0];
            for (int i = 1; i < arr.Length; i++)
            {
                if (arr[i] < min)
                {
                    min = arr[i];
                }
            }
            return min;
        }
        public static Edge SelectEdge(Edge[] edgelist)
        {
            List<Edge> selectEdges = new List<Edge>();

            foreach (Edge edge in edgelist)
            {
                Point3d vertex1, vertex2;
                edge.GetVertices(out vertex1, out vertex2);
                double z1 = vertex1.Z;
                double z2 = vertex2.Z;

                if (z1>0 && z2>0)
                {
                    selectEdges.Add(edge);
                }
            }

            Edge edge0 = selectEdges[0];
            var length0 = edge0.GetLength();
            foreach (Edge edge in selectEdges)
            {
                var Length = edge.GetLength();
                if (Length > length0)
                {
                    edge0 = edge;
                    length0 = Length;
                }
            }

            return edge0;
        }
        public static void Main(string[] args)

        {
            theProgram = new Utility_2();

            var bodies = GetBody();

            Body body1 = bodies[0];
            Edge[] edges = body1.GetEdges();
            Edge seleceEdges = SelectEdge(edges);


            //将edge转化为curve

            //method 1
            //Section section_edge = workPart.Sections.CreateSection(selectedEdge);
            //Curve curve = workPart.Curves.CreateSmartCompositeCurve(section_edge, SmartObject.UpdateOption.WithinModeling, 0.001);

            //method 2
            Tag edgearctag;
            theUFSession.Modl.CreateCurveFromEdge(seleceEdges.Tag, out edgearctag);
            //Line edgecurve = (Line)NXObjectManager.Get(edgearctag);
            //Curve offsetcurve = OffsetCurve(edgecurve, 4);



            //展示文字
            theSession.ListingWindow.Open();
            theSession.ListingWindow.WriteLine(string.Format("{0}", 1));

            //DeleteObject(edgeCurve);






            //System.Windows.Forms.MessageBox.Show(a.ToArray().Length.ToString());
        } 
        
    
    
    }

}

