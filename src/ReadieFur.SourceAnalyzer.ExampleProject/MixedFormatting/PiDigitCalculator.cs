using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.ExampleProject.MixedFormatting
{
    internal class PiDigitCalculator
    {
        void Main()
        {
            Console.WriteLine("Enter the number of digits of Pi to calculate:");
            int n = Convert.ToInt32(Console.ReadLine());

            double pi = 4;
            bool subtract = false;
            for (int i = 3; i < n * 2; i += 2)
            {
                if (subtract) pi -= 4 / i;
                else pi += 4 / i;
                subtract = !subtract;
            }
            Console.WriteLine("Value of Pi:" + pi.ToString());
        }
    }
}
