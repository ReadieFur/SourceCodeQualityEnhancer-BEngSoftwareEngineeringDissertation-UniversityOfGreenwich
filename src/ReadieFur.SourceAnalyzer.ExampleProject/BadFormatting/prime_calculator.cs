using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.ExampleProject.BadFormatting
{
    class prime_calculator {
void Main() {
Console.WriteLine("Enter a number:");
int num=Convert.ToInt32(Console.ReadLine());

bool isPrime=true;
if(num<=1)
isPrime=false;
else{
for(int i=2;i<num;i++) {
if(num%i==0) isPrime=false;break;
}
}
Console.WriteLine("Is prime:"+isPrime.ToString());
}
}
}
