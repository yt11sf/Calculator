using System;
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
                    Console.WriteLine($"Answer = {i2p.Evaluate()}");
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

        /*
         * Return details of how to use the calculator
         * return string
         */
        private static string Help()
        {
            string[] functionList = {
                "Addition","+", "Subtraction","-", "Multiplication","*", "Division","/",
                "Sine", "sin", "Cosine", "cos", "Tangent", "tan",
                "Inverse sine", "sinh", "Inverse cosine", "cosh", "Inverse tangent", "tanh" };
            StringBuilder s = new StringBuilder("---***--- Help ---***---\n");
            s.AppendLine("Enter '-h' to see this menu.");
            s.AppendLine("Enter 'exit' or press 'Ctrl' + 'c' to exit the calculator.");
            s.AppendLine("\n*** Rules ***");
            s.AppendLine("Nested equations using brackets '(' and ')' are allowed but brackets must be closed.");
            s.AppendLine("Trigonometry function use radian by default.");
            s.AppendLine("-  Append \"deg\" for degree or \"rad\" for radian behind the numbers to specify.");
            s.AppendLine("-  To ensure that degree is correct, enclose the operand of trigonometry with brackets before entering \"deg\"");
            s.AppendLine("---  Ex: sin(90^2)deg");
            s.AppendLine("Lograithm is base 10 by default.");
            s.AppendLine("-  Logarithm must be entered in the form of \"log_b(x)\", where \"b\" stand for base, \"x\" stand for value.");


            s.AppendLine("\nAvailable Functions:");
            for (int i = 0; i < functionList.Length; i += 2)
            {
                s.AppendFormat("{0,-15} : {1,-10}\n", functionList[i], functionList[i + 1]);
            }
            s.AppendLine("---***--- End of Help ---***---");

            return s.ToString();
        }
    }

    class InfixToPostfixCalculator
    {
        private string s;
        private string[] sArr;
        private List<string> output;
        private Stack<OperClass> operStack;
        private class OperClass
        {
            public string oper { get; set; }
            public int precedence { get; set; }
        }

        public InfixToPostfixCalculator(string s)
        {
            this.operStack = new Stack<OperClass>();
            this.output = new List<string>();
            this.s = RemoveWhiteSpace(s.ToLower());
            this.sArr = Separator(this.s);
        }

        /*
         * Prevent floating point problem
         * @param double num: a number
         * @param in precision: round given number to specified decimal point
         * return double
         */
        private double RoundDouble(double num, int precision = 14)
        {
            return Math.Round(num, precision);
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
         * Separate equation by operand and operator into a string array 
         * @param string input: a math equation
         * return string array
         */
        private static string[] Separator(string input)
        {
            // Regex.Split added empty string before open paranthesis and after close paranthesis (something capturing group)
            List<string> separated = Regex.Split(input, @"((?<![Ee])[+-]|[*()^/%]|mod|sinh?|cosh?|tanh?|log(?:_\d+)?|deg|rad)").Where(s => s != "").ToList();
            if (Regex.IsMatch(separated[0], @"\+|\-")) separated.Insert(0, "0");
            // add '*' operator when needed
            for (int i = 1; i < separated.Count; i++)
                //if (Regex.IsMatch(separated[i], @"\=")) ; // when variable is added into equation
                if (Regex.IsMatch(separated[i], @"\(|sinh?|cosh?|tanh?|log(_\d+)?|^\w$") && Regex.IsMatch(separated[i - 1], @"\)|^\d+$|^\w$")) separated.Insert(i, "*");
            return separated.ToArray();
        }

        /*
         * Transform infix equation to postfix equation
         * return void
         */
        private void Transform()
        {
            for (int i = 0; i < this.sArr.Length; i++)
            {
                // '+' or '-'
                if (Regex.IsMatch(this.sArr[i], @"^[+-]$")) Oper(this.sArr[i], 1);
                // '*' or '/'
                else if (Regex.IsMatch(this.sArr[i], @"^([*/%]|mod)$")) Oper(this.sArr[i], 2);
                // trigonometry or logarithm
                else if (Regex.IsMatch(this.sArr[i], @"^(sinh?|cosh?|tanh?|log(_\d+)?)$")) Oper(this.sArr[i], 3);
                // power
                else if (Regex.IsMatch(this.sArr[i], @"^\^$")) Oper(this.sArr[i], 4);
                // User specified deg/rad
                else if (Regex.IsMatch(this.sArr[i], @"^(deg|rad)$")) Oper(this.sArr[i], 9);
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
                                this.output.Add(Convert.ToString(innerValue.Evaluate()));
                                i = j; // adjust outer index to after close bracket
                                break;
                            }
                        }
                        // in case more open brackets found
                        else if (this.sArr[j] == "(") openBracket++;
                    }
                    if (openBracket != 0) throw new InvalidOperationException("Bracket is not closed");
                }
                // Add number onto output
                else this.output.Add(this.sArr[i]);
            }
            // Add all remaining operator to output
            while (this.operStack.Count() != 0) this.output.Add(this.operStack.Pop().oper);
        }

        /*
         * Push operator to stack according to their precedence
         * @param string input: an operator
         * @param int precedence: precedence of given operator
         * return void
         */
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
                else this.output.Add(top.oper);
            }
            this.operStack.Push(new OperClass() { oper = input, precedence = precedence });
        }

        /*
         * Evaluate the equation after transforming it to postfix
         * return double
         */
        public double Evaluate()
        {
            this.Transform();
            /*foreach (var oper in this.output) { Console.Write($"{oper} ,"); }
            Console.WriteLine();*/
            Stack<double> evalStack = new Stack<double>();
            double result, num1, num2;
            for (int i = 0; i < this.output.Count; i++)
            {
                /*Console.Write($"Working on : {this.output[i]}\nevalStack: ");
                foreach (var ev in evalStack) { Console.Write($"{ev} ,"); }
                Console.WriteLine();*/
                // Addition, Subtraction or Multiplication
                if (Regex.IsMatch(this.output[i], @"^([*/%^+-]|mod)$"))
                {
                    num2 = evalStack.Pop();
                    num1 = evalStack.Pop();
                    switch (this.output[i])
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
                    evalStack.Push(RoundDouble(result));
                }
                // Trigonometry
                else if (Regex.IsMatch(this.output[i], @"^(sinh?|cosh?|tanh?)$"))
                {
                    num1 = evalStack.Pop();
                    switch (this.output[i])
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
                    evalStack.Push(RoundDouble(result));
                }
                // Logarithm
                else if (Regex.IsMatch(this.output[i], @"^log(_\d+)?$"))
                {
                    double b = 10; // base = 10 by default
                    // if user specified base
                    Match mc = Regex.Match(this.output[i], @"\d+");
                    if (mc.Value != "") b = Convert.ToDouble(mc.Value);

                    result = Math.Log(Convert.ToDouble(evalStack.Pop()), b);
                    evalStack.Push(RoundDouble(result));
                }
                // Convert degree or radiant
                else if (Regex.IsMatch(this.output[i], @"^(deg|rad)$"))
                {
                    num1 = Convert.ToDouble(evalStack.Pop());
                    // Round to prevent precision lost
                    if (this.output[i] == "deg") result = RoundDouble(num1 * Math.PI) / 180.0;
                    else if (this.output[i] == "rad") result = num1;
                    else throw new Exception("Somehow trying to convert deg/rad even though not specified");
                    evalStack.Push(RoundDouble(result));
                }
                // Push number onto evaluation stack
                else evalStack.Push(Convert.ToDouble(this.output[i]));
            }
            //Console.WriteLine($"evaluation: {evalStack.Peek()}");
            return Convert.ToDouble(evalStack.Pop());
        }
    }
}
