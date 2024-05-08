using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.ExampleProject.BadFormatting
{
    internal class Compact
    {
        class prime_calculator { public bool Calculate(int num) { if (num <= 1) return false; else { for (int i = 2; i < num; i++) { if (num % i == 0) return false; } } return true; } }

        class pi_digit_calculator { public double Calculate(int n) { double pi = 4; bool subtract = false; for (int i = 3; i < n * 2; i += 2) { if (subtract) pi -= 4 / i; else pi += 4 / i; subtract = !subtract; } return pi; } }
    }
}
