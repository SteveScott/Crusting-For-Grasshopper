using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Geometry;
using Grasshopper.Kernel.Geometry.Delaunay;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using rg = Rhino.Geometry;

namespace Crusting
{
    public class CheesemakerFromVoronoiComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public CheesemakerFromVoronoiComponent()
          : base("CheesemakerFromVoronoi", "VCheese",
            "The Point Cloud Crust 'Cheesemaker' algorithm optimized by taking the Delaunay triangulation tetrahedrons as an input in addition to the points.",
            "Crusts", "")
        {

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("cells", "C", "a collection of tetrahedron cells from the delauney triangulation", GH_ParamAccess.list);
            pManager.AddPointParameter("input", "P", "input points from the delauney cells", GH_ParamAccess.list);
            pManager.AddNumberParameter("maxD", "D", "maximum length of a side of a triangle", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "M", "the point cloud crust", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            List<Mesh> inputCells = new List<Mesh>();
            List<Point3d> inputPoints = new List<Point3d>();
            double maxDistance = 0;
            var outputMeshes = new List<Mesh>();
            if (!DA.GetDataList(0, inputCells)) return ;
            if (!DA.GetDataList(1, inputPoints)) return ;
            if (!DA.GetData(2, ref  maxDistance)) return ;
            foreach (Mesh t in inputCells) 
            {  
                MeshFaceList faces = t.Faces;
                var vs = t.Vertices;

                foreach (var face in t.Faces) 
                {
                   var firstPoint = new Point3d(vs[face.A]);
                   var secondPoint = new Point3d(vs[face.B]);
                   var thirdPoint = new Point3d(vs[face.C]);
                    //sometimes it draws the sphere in, sometimes it draws the sphere out. Draw it in and out by adding points in the direction of the normal and the reverse of the normal.
                    rg.Plane thisPlane = new rg.Plane(firstPoint, secondPoint, thirdPoint);
                    Vector3d thisNormal = thisPlane.Normal;

                    thisNormal = rg.Vector3d.Multiply(thisNormal, 1.001);
                    var xformIn = Transform.Translation(thisNormal);

                    thisNormal.Reverse();
                    var xformOut = Transform.Translation(thisNormal);
                        

                    bool shouldMakeOut = ShouldIMakeATriangle(inputPoints, firstPoint, secondPoint, thirdPoint, xformOut);
                    bool shouldMakeIn = ShouldIMakeATriangle(inputPoints, firstPoint, secondPoint, thirdPoint, xformIn);
                    if (shouldMakeOut || shouldMakeIn)
                    {
                        Mesh thisTriangle = new Triangle3d(firstPoint, secondPoint, thirdPoint).ToMesh();
                        outputMeshes.Add(thisTriangle);
                    }
                
                }
                
            }
            Mesh answer = new Mesh();
            //Debug.Assert(outputMeshes.Count > 0);
            foreach (Mesh mesh in outputMeshes)
            {
                answer.Append(mesh);
            }
            DA.SetData(0, answer);

            bool ShouldIMakeATriangle(List<Point3d> inputPointsLocal, Point3d firstPoint, Point3d secondPoint, Point3d thirdPoint, Transform xform)
            {
                
                Point3d offsetA = new Point3d(firstPoint);
                Point3d offsetB = new Point3d(secondPoint);
                Point3d offsetC = new Point3d(thirdPoint);

                offsetA.Transform(xform);
                offsetB.Transform(xform);
                offsetC.Transform(xform);
                List<Point3d> thesePoints = new List<Point3d>() { firstPoint, secondPoint, thirdPoint, offsetA, offsetB, offsetC };
                bool isInside = false;
                Brep thisSphere2 = Rhino.Geometry.Sphere.FitSphereToPoints(thesePoints).ToBrep();

                foreach (Point3d p in inputPointsLocal)
                {
                    if ((firstPoint.DistanceTo(secondPoint) > maxDistance) || (secondPoint.DistanceTo(thirdPoint) > maxDistance) || (thirdPoint.DistanceTo(firstPoint) > maxDistance))
                            { return false; }
                    //the points in the triangle should not be included, or nothing will come of it.
                    if (p.DistanceTo(firstPoint) < 0.1 || p.DistanceTo(secondPoint) < 0.1 || p.DistanceTo(thirdPoint) < 0.1)
                    { continue; }
                    
                    //if there are points inside the sphere, it is not the crust. Do not add geometry
                    if (thisSphere2.IsPointInside(p, 0, true))
                    {
                        isInside = true;
                        break;
                    }
                }
                if (!isInside)
                {
                    //it is a crust. Add the face
                    return true;
                }
                else
                    return false;
            }
        }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon => null;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
    public override Guid ComponentGuid =>  new Guid("A1492503-D3F4-454F-839B-05AF4358A3D7"); }

 


}