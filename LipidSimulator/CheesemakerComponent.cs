using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Geometry.Delaunay;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using rg = Rhino.Geometry;
namespace Crusting
{
    public class CheesemakerComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CheesemakerComponent()
          : base("Cheesemaker", "Cheesemaker",
            "Description",
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
            pManager.AddNumberParameter("Max Distance", "D", "Maximum distance for triangles. mall values filter the maximum triangle size, dramatically imporving performance without altering the mesh (unless it is too small). ", GH_ParamAccess.item, 3.0);
            pManager.AddNumberParameter("X Cor", "x", "the X coordinates of your point cloud.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Y Cor", "y", "the Y coordinates of your point cloud.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Z Cor", "z", "the Z coordinates of your point cloud.", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Output Mesh", "M", "Output after triangulation.",GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
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
                    return true;
                }
                else
                    return false;
            }

            List<double> xs = new List<double>();
            List<double> ys = new List<double>();
            List<double> zs = new List<double>();
            double maxDistance = 0;
            List<Mesh> outputMeshes = new List<Mesh>();
            Mesh answer = new Mesh();
            List<Rhino.Geometry.Point3d> inputPoints = new List<Rhino.Geometry.Point3d>();
            List<Point3d> verticies = new List<Point3d>();
            List<MeshFace> faces = new List<MeshFace>();

            if (!DA.GetData(0, ref maxDistance)) return;
            if (!DA.GetDataList(1, xs)) return;
            if (!DA.GetDataList(2, ys)) return;
            if (!DA.GetDataList(3, zs)) return;
            Debug.Assert(xs.Count == ys.Count, "length of X doesn't match length of Y");
            Debug.Assert(xs.Count == zs.Count, "length of X doesn't match length of Z");
            for (int i = 0; i < xs.Count; i++)
            {
                inputPoints.Add(new Point3d(xs[i], ys[i], zs[i]));
            }
            //Point Cloud Crust "Cheesemaker" Algorithm. 
            //Select three points at random that aren't too far apart.
            foreach (Point3d i in inputPoints)
            {
                foreach (Point3d j in inputPoints)
                {
                    foreach (Point3d k in inputPoints)
                    {
                        //because it is the order of N^3, we need to reduce the number of points to look at by omitting obviously large triangles that are far apart.
                        if (i != j && j != k && k != i && i.DistanceTo(j) < maxDistance && j.DistanceTo(k) < maxDistance)
                        {
                            var thesePoints = new List<Point3d>();
                            thesePoints.Add(i);
                            thesePoints.Add(j);
                            thesePoints.Add(k);
                            var fourthPoint = new Point3d(i);

                            Transform xformOut = new Transform();
                            xformOut.M00 = 1.1;
                            xformOut.M11 = 1.1;
                            xformOut.M22 = 1.1;
                            xformOut.M33 = 1;
                            
                            fourthPoint.Transform(xformOut);
                            
                            thesePoints.Add(fourthPoint); 
                            bool isInside = false;
                            //generate a sphere formed by the three points.
                            Rhino.Geometry.Brep thisSphere = Rhino.Geometry.Sphere.FitSphereToPoints(thesePoints).ToBrep();
                            foreach (Point3d p in inputPoints)
                            {//the points in the triangle should not be included, or nothing will come of it.
                                if (p == i || p == j || p == k)
                                    { continue; }
                                //if there are points inside the sphere, it is not the crust. Do not add geometry
                                //sometimes it draws the sphere in, sometimes it draws the sphere out. Draw it in and out by adding points in the direction of the normal and the reverse of the normal.
                                rg.Plane thisPlane = new rg.Plane(i, j, k);
                                Vector3d thisNormal = thisPlane.Normal;

                                thisNormal = rg.Vector3d.Multiply(thisNormal, 1.001);
                                var xformIn = Transform.Translation(thisNormal);

                                thisNormal.Reverse();
                                var xformOut2 = Transform.Translation(thisNormal);


                                bool shouldMakeOut = ShouldIMakeATriangle(inputPoints, i, j, k, xformOut2);
                                bool shouldMakeIn = ShouldIMakeATriangle(inputPoints, i, j, k, xformIn);
                                if (shouldMakeOut || shouldMakeIn)
                                {
                                    Mesh thisTriangle = new Triangle3d(i,j, k).ToMesh();
                                    outputMeshes.Add(thisTriangle);
                                }
                            }

                            if (!isInside)
                            //it is a crust. Add the face
                            {
                                Mesh thisTriangle = new Triangle3d(i, j, k).ToMesh();
                                outputMeshes.Add(thisTriangle);
                            }
                        }
                    }
                }
            }
            answer = new Mesh();
            foreach (Mesh mesh in outputMeshes)
            {
                answer.Append(mesh);
            }
            //Debug.Assert(answer.IsValid);
            DA.SetData(0, answer); 
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("17141BD6-4F41-43B2-8761-D0C879255DAB");


    }
}