using System;
using System.Collections.Generic;
using System.Diagnostics;
using Grasshopper.GUI;
using Grasshopper.Kernel;
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
            pManager.AddNumberParameter("maxDistance", "D", "Maximum polygon face size.", GH_ParamAccess.item);
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
            double maxDistance = double.PositiveInfinity;
            var outputMeshes = new List<Mesh>();
            if (!DA.GetDataList(0, inputCells)) return ;
            if (!DA.GetDataList(1, inputPoints)) return ;
            if(!DA.GetData(2, ref maxDistance)) { maxDistance = double.PositiveInfinity; };
            foreach (Mesh t in inputCells) 
            {  
                MeshFaceList faces = t.Faces;
                var vs = t.Vertices;

                foreach (var face in t.Faces) 
                {
                   var firstPoint = vs[face.A];
                   var secondPoint = vs[face.B];
                   var thirdPoint = vs[face.C];
                   Debug.Assert(firstPoint.GetType() == typeof(Point3d), "point is not a Point3d");
                   if (firstPoint.DistanceTo(secondPoint) < maxDistance && secondPoint.DistanceTo(thirdPoint) < maxDistance)
                    {
                        continue;
                    }
                   var thesePoints2 = new List<Point3d>();
                   thesePoints2.Add(firstPoint);
                   thesePoints2.Add(secondPoint);
                   thesePoints2.Add(thirdPoint);
                   Rhino.Geometry.Brep thisSphere2 = Rhino.Geometry.Sphere.FitSphereToPoints(thesePoints2).ToBrep();
                   bool isInside = false;
                   foreach (Point3d p in inputPoints)
                    {//the points in the triangle should not be included, or nothing will come of it.
                        if (p.DistanceTo(firstPoint) < 0.1 && p.DistanceTo(secondPoint) < 0.1 || p.DistanceTo(thirdPoint) < 0.1)
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