using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Calculator
{
    class Calculator
    {
        static void Main(string[] args)
        {
            Console.WriteLine("***Simple Calculator***");
            string input;
            while (true)
            {
                Console.Write("Input ('-h' for help): ");
                input = Console.ReadLine();
                if (input == "exit")
                {
                    Console.WriteLine("Thanks for using Simple Calculator...");
                    break;
                }
                else if (input == "-h")
                {
                    Console.WriteLine(Help());
                    continue;
                }
                try
                {
                    InfixToPostfixCalculator i2p = new InfixToPostfixCalculator(input);
                    Console.WriteLine($"Answer = {i2p.Evaluation()}");
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine("Check Equation!");
                    Console.WriteLine(ex);
                }
                catch (NotSupportedException ex)
                {
                    Console.WriteLine(ex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unknown Exception:");
                    Console.WriteLine(ex);
                }
            }
        }

        private static string Help()
        {
            string[] functionList = {
                "Addition","+", "Subtraction","-", "Multiplication","*", "Division","/",
                "Sine", "sin", "Cosine", "cos", "Tangent", "tan",
                "Inverse sine", "sinh", "Inverse cosine", "cosh", "Inverse tangent", "tanh" };
            StringBuilder s = new StringBuilder("*** Help ***\n");
            s.AppendLine("Enter '-h' to see this menu.");
            s.AppendLine("Enter 'exit' or press 'Ctrl' + 'c' to exit the calculator.");
            s.AppendLine("Nested equations using brackets '(' and ')' are allowed but open bracket must be closed.");
            s.AppendLine("Trigonometry function use radian by default. Append \"deg\" for degree or \"rad\" for radian behind the numbers to specify. ");


            s.AppendLine("Available Functions:");
            for (int i = 0; i < functionList.Length; i += 2)
            {
                s.AppendFormat("{0,-15} : {1,-10}\n", functionList[i], functionList[i + 1]);
            }
            s.AppendLine("*** End of Help ***");

            return s.ToString();
        }
    }


}

class InfixToPostfixCalculator
{
    private string s;
    private string[] sArr;
    private Queue<string> output;
    private Stack<OperClass> operStack;
    private class OperClass
    {
        public string oper { get; set; }
        public int precedence { get; set; }
    }

    public InfixToPostfixCalculator(string s)
    {
        // (1+2^3) *4e5/6 sin7
        this.operStack = new Stack<OperClass>();
        this.output = new Queue<string>();
        this.s = RemoveWhiteSpace(s.ToLower());
        this.sArr = Separator(this.s);
        /*foreach (string item in this.sArr) { Console.Write($"{item} , "); } Console.WriteLine();*/
        //foreach (string item in this.sArr) if (!Validate(item)) throw new InvalidOperationException("Input is invalid");
    }

    /*  
    *  Remove all white spaces in a string
    *  @param string s: input string
    *  return string
    *  
    *  Reference from https://stackoverflow.com/a/14591148
    *  Notes: 
    *  1. using ToCharArray is faster than using .Where() directly on the string. This has something to do with the overhead into the IEnumerable<> in each iteration step, and the ToCharArray being very efficient (block-copy) and the compiler optimizes iteration over arrays
    *  2.  If you say you had a digitized version of a Volume on US Tax Law (~million words?), with a handful of iterations, Regex is king, by far! Its not what is faster, but what should be used in which circumstance
    */
    private static string RemoveWhiteSpace(string input)
    {
        return new string(input.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray());
    }

    /*
    public static bool Validate(string input)
    {
        //! does not check for closing brackets (Handled), value for trigonometry function etc
        //Console.WriteLine(input + ":" + Regex.IsMatch(input, @"(sin)*(cos)*(tan)*[eE\d\(\)\.+\-\*\^\/]"));
        return Regex.IsMatch(input, @"(sinh?)|(cosh?)|(tanh?)|[eE\d\(\)\.+\-\*\^\/]");
    }*/

    private static string[] Separator(string input)
    {
        //! Regex added empty string before open paranthesis, and after close paranthesis
        // Regex pattern reference: https://stackoverflow.com/a/4680185
        List<string> separated = Regex.Split(input, @"((?<![Ee])\+|\-|\*|\(|\)|\^|\/|\%|mod|sinh?|cosh?|tanh?)").Where(s => s != "" && s != " ").ToList();
        for (int i = 1; i < separated.Count; i++)
            if (Regex.IsMatch(separated[i], @"\(|sinh?|cosh?|tanh?") && Regex.IsMatch(separated[i - 1], @"\)|^\d+$")) separated.Insert(i, "*");

        return separated.ToArray();
    }

    private void Transition()
    {
        for (int i = 0; i < this.sArr.Length; i++)
        {
            // '+' or '-'
            if (Regex.IsMatch(this.sArr[i], @"\+|\-")) Oper(this.sArr[i], 1);
            // '*' or '/'
            else if (Regex.IsMatch(this.sArr[i], @"\*|\/|\%|mod|\^")) Oper(this.sArr[i], 2);
            // trigonometry
            else if (Regex.IsMatch(this.sArr[i], @"sinh?|cosh?|tanh?")) Oper(this.sArr[i], 3);
            // Open bracket found
            else if (this.sArr[i] == "(")
            {
                int openBracket = 1;
                // Find respective close bracket
                for (int j = i + 1; j < this.sArr.Length; j++)
                {
                    if (this.sArr[j] == ")")
                    {
                        openBracket--;
                        // Solve bracket recursively
                        if (openBracket == 0)
                        {
                            InfixToPostfixCalculator innerValue = new InfixToPostfixCalculator(string.Join("", this.sArr, i + 1, j - i - 1));
                            this.output.Enqueue(Convert.ToString(innerValue.Evaluation()));
                            i = j; // adjust outer index to after close bracket
                            break;
                        }
                    }
                    // in case more open brackets found
                    else if (this.sArr[j] == "(") openBracket++;
                }
                if (openBracket != 0) throw new InvalidOperationException("Bracket is not closed");
            }
            // Ignore close bracket as it should have been solved
            //else if (this.sArr[i] == ")") continue;
            else this.output.Enqueue(this.sArr[i]);
        }
        while (this.operStack.Count() != 0) this.output.Enqueue(this.operStack.Pop().oper);
    }

    private void Oper(string input, int precedence)
    {
        while (this.operStack.Count() != 0)
        {
            OperClass top = this.operStack.Pop();
            if (precedence > top.precedence)
            {
                this.operStack.Push(top);
                break;
            }
            else this.output.Enqueue(top.oper);

        }
        this.operStack.Push(new OperClass() { oper = input, precedence = precedence });
    }

    public double Evaluation()
    {
        this.Transition();
        /*foreach (var oper in this.output)
        {
            Console.Write($"{oper} ,");
        }
        Console.WriteLine();*/
        Stack<string> evalStack = new Stack<string>();
        double result, num1, num2;
        foreach (string oper in this.output)
        {
            /*Console.WriteLine($"Working on : {oper}\nevalStack: ");
            foreach (var ev in evalStack)
            {
                Console.Write($"{ev} ,");
            }
            Console.WriteLine();*/
            if (Regex.IsMatch(oper, @"\+|\-|\*|\/|\^"))
            {
                num2 = Convert.ToDouble(evalStack.Pop());
                num1 = Convert.ToDouble(evalStack.Pop());
                switch (oper)
                {
                    case "+":
                        result = num1 + num2;
                        break;
                    case "-":
                        result = num1 - num2;
                        break;
                    case "*":
                        result = num1 * num2;
                        break;
                    case "/":
                        result = num1 / num2;
                        break;
                    case "^":
                        result = Math.Pow(num1, num2);
                        break;
                    case "%":
                    case "mod":
                        result = num1 % num2;
                        break;
                    default:
                        throw new NotSupportedException("Operation is not implemented");
                }
                evalStack.Push(Convert.ToString(result));
            }
            else if (Regex.IsMatch(oper, @"(sinh?)|(cosh?)|(tanh?)"))
            {
                string temp = evalStack.Pop();
                string end = temp.Substring(temp.Length - 3);
                if (end == "deg")
                {
                    num1 = Convert.ToDouble(temp.Substring(0, temp.Length - 3)) * Math.PI / 180.0;
                }
                else if (end == "rad")
                {
                    num1 = Convert.ToDouble(temp.Substring(0, temp.Length - 3));
                }
                else
                {
                    num1 = Convert.ToDouble(temp);
                }
                switch (oper)
                {
                    case "sin":
                        result = Math.Sin(num1);
                        break;
                    case "cos":
                        result = Math.Cos(num1);
                        break;
                    case "tan":
                        result = Math.Tan(num1);
                        break;
                    case "sinh":
                        result = Math.Sinh(num1);
                        break;
                    case "cosh":
                        result = Math.Cosh(num1);
                        break;
                    case "tanh":
                        result = Math.Tanh(num1);
                        break;
                    default:
                        throw new NotSupportedException("Operation is not implemented");
                }
                evalStack.Push(Convert.ToString(result));
            }
            else evalStack.Push(oper);
        }
        //Console.WriteLine($"evaluation: {evalStack.Peek()}");
        return Convert.ToDouble(evalStack.Pop());
    }
}
