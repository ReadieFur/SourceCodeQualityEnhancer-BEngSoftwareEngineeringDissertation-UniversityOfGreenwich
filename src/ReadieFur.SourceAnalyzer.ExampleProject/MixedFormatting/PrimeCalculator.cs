using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.ExampleProject.MixedFormatting
{
    internal class PrimeCalculator
    {
        void Main()
        {
            Console.WriteLine("Enter a number:");
            int num = Convert.ToInt32(Console.ReadLine());

            bool is_prime = true;
            if (num <= 1)
                is_prime = false;
            else
            {
                for (int i = 2; i < num; i++)
                {
                    if (num % i == 0) is_prime = false; break;
                }
            }
            Console.WriteLine("Is prime:" + is_prime.ToString());
        }
    }
}
