using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;


namespace HeavyScience
{
    class Utilities
    {
        private static readonly System.Random _random = new System.Random();

        public static bool isSurfaceSample(ScienceData scienceData)
        {
            bool SampleCheck = false;
            string[] listOfSampleStrings = { "surfaceSample", "cometSample", "asteroidSample", "ROCScience" };
            SampleCheck = listOfSampleStrings.Any(scienceData.subjectID.Contains);

            //Check ROCScience and set false for anything not pickupable
            Match result = Regex.Match(scienceData.subjectID, @"^.*?(?=@)");
            //scatterLibrary ScatterItem = scatterBuilder.scatterLib.Find(x => x.bodyScatterID.Equals(result.Value));
            if (scatterBuilder.scatterLib.ContainsKey(result.Value))
                SampleCheck = scatterBuilder.scatterLib[result.Value].isCollectable; //ScatterItem.isCollectable;
            return SampleCheck;
        }
        public static bool IsBetween(double testValue, double bound1, double bound2)
        {
            if (bound1 > bound2)
                return testValue >= bound2 && testValue <= bound1;
            return testValue >= bound1 && testValue <= bound2;
        }
        // Generates a random number within a range.     
        public static int RandomNumber(int min, int max)
        {
            return _random.Next(min, max);
        }
        public static float RandomNumber(float min, float max)
        {
            return (_random.Next((int)(min * 1000), (int)(max * 1000))) / 1000;
        }
    }
}
