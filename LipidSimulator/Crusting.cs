using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Crusting
{
    public class Crusting : GH_AssemblyInfo
    {
        public override string Name => "Cheesemaker";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "At least one algorithm to create a crust out of point clouds in Grasshopper.";

        public override Guid Id => new Guid("e8e2985f-94eb-4131-a230-2d679e4d08a0");

        //Return a string identifying you or your company.
        public override string AuthorName => "Steve Scott";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "stevescott517@gmail.com";
    }
}