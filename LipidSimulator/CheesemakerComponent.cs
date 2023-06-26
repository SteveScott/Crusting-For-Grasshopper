using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
                        if (i != j && j != k && k != i && i.DistanceTo(j) < maxDistance && j.DistanceTo(k) < maxDistance)
                        {
                            var thesePoints = new List<Point3d>();
                            thesePoints.Add(i);
                            thesePoints.Add(j);
                            thesePoints.Add(k);

                            bool isInside = false;
                            //generate a sphere formed by the three points.
                            Rhino.Geometry.Brep thisSphere = Rhino.Geometry.Sphere.FitSphereToPoints(thesePoints).ToBrep();
                            foreach (Point3d p in inputPoints)
                            {
                                if (p == i || p == j || p == k)
                                    { continue; }   
                                //if there are points inside the sphere, it is not the crust. Do not add geometry
                                if (thisSphere.IsPointInside(p, 0, true))
                                {
                                    isInside = true;
                                    break;
                                }

                            }

                            if (!isInside)
                            //it is a crust. Add the face
                            {
                                /*
                                
                                answer.Vertices.AddVertices(thesePoints);

                              
                                var edges = new List<List<Rhino.Geometry.Point3d>>();
                                edges.Add(new List<Point3d> { i, j });
                                edges.Add(new List<Point3d> { i, k });
                                edges.Add(new List<Point3d> { j, k });
                                var plane = new Rhino.Geometry.Plane(i, j, k);
                                var mesh = Rhino.Geometry.Mesh.CreateFromTessellation(thesePoints, edges, plane, false);
                                */
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