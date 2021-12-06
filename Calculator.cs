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
                Console.Write("Input ('help' for help): ");
                input = Console.ReadLine();
                if (input == "exit")
                {
                    Console.WriteLine("Thanks for using Simple Calculator...");
                    break;
                }
                else if (input == "help")
                {
                    Console.WriteLine(Help());
                    continue;
                }
                InfixToPostfixCalculator i2p = new InfixToPostfixCalculator(input);
                Console.WriteLine(i2p.Evaluation());
            }
        }

        private static string Help()
        {
            string[] functionList = {
                "Addition","+", "Subtraction","-", "Multiplication","*", "Division","/",
                "Sine", "sin", "Cosine", "cos", "Tangent", "tan",
                "Inverse sine", "sinh", "Inverse cosine", "cosh", "Inverse tangent", "tanh" };
            StringBuilder s = new StringBuilder("*** Help ***\n");
            s.AppendLine("Enter 'help' to see this menu.");
            s.AppendLine("Enter 'exit' or press 'Ctrl' + 'c' to exit the calculator.");
            s.AppendLine("Nested equations using brackets '(' and ')' are allowed.");
            s.AppendLine("Every open bracket must be closed.");
            s.AppendLine("'*' must be included before trigonometry (ie: 2*sin90)");

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
    private readonly string s;
    private readonly string[] sArr;
    private readonly Queue<string> output;
    private readonly Stack<OperClass> stack;
    private class OperClass
    {
        public string oper { get; set; }
        public int precedence { get; set; }
    }

    public InfixToPostfixCalculator(string s)
    {
        // (1+2^3) *4e5/6 sin7
        //this.s = removeWhiteSpace(s);
        this.stack = new Stack<OperClass>();
        this.output = new Queue<string>();
        this.s = RemoveWhiteSpace(s.ToLower());
        this.sArr = Separator(this.s);
        /*foreach (string item in this.sArr)
        {
            Console.Write($"{item} , ");
        }
        Console.WriteLine();*/
        foreach (string item in this.sArr) if (!Validate(item)) throw new InvalidOperationException("Input is invalid");
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
    //! Not used until fixed separator
    private static string RemoveWhiteSpace(string input)
    {
        return new string(input.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray());
    }

    public static bool Validate(string input)
    {
        //! does not check for closing brackets (Handled), value for trigonometry function etc
        //Console.WriteLine(input + ":" + Regex.IsMatch(input, @"(sin)*(cos)*(tan)*[eE\d\(\)\.+\-\*\^\/]"));
        return Regex.IsMatch(input, @"(sinh?)|(cosh?)|(tanh?)|[eE\d\(\)\.+\-\*\^\/]");
    }

    //! Regex fix needed
    private static string[] Separator(string input)
    {
        //! Regex added empty string before open paranthesis, and after close paranthesis
        // Regex pattern reference: https://stackoverflow.com/a/4680185
        List<string> separated = Regex.Split(input, @"((?<![Ee])\+|\-|\*|\(|\)|\^|\/|\%|mod|sinh?|cosh?|tanh?)").Where(s => s != "" && s != " ").ToList();
        for (int i = 1; i < separated.Count; i++)
        {
            if (Regex.IsMatch(separated[i], @"\(|sinh?|cosh?|tanh?") && Regex.IsMatch(separated[i - 1], @"\)|^\d+$"))
            {
                separated.Insert(i, "*");
            }
        }
        //Console.WriteLine();

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
            else if (Regex.IsMatch(this.sArr[i], @"sinh?|cosh?|tanh?")) Oper(this.sArr[i], 9);
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
        while (this.stack.Count() != 0) this.output.Enqueue(this.stack.Pop().oper);
    }

    private void Oper(string input, int precedence)
    {
        while (this.stack.Count() != 0)
        {
            OperClass top = this.stack.Pop();
            if (precedence > top.precedence)
            {
                this.stack.Push(top);
                break;
            }
            else this.output.Enqueue(top.oper);

        }
        this.stack.Push(new OperClass() { oper = input, precedence = precedence });
    }

    public double Evaluation()
    {
        this.Transition();
        Stack<string> evalStack = new Stack<string>();
        double result, num1, num2;
        foreach (var item in this.output)
        {
            Console.Write(item + " , ");
        }
        Console.WriteLine();
        foreach (string item in this.output)
        {
            if (Regex.IsMatch(item, @"\+|\-|\*|\/"))
            {
                num2 = Convert.ToDouble(evalStack.Pop());
                num1 = Convert.ToDouble(evalStack.Pop());
                switch (item)
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
                        throw new InvalidOperationException("Operation is not implemented");
                }
                evalStack.Push(Convert.ToString(result));
            }
            else if (Regex.IsMatch(item, @"(sinh?)|(cosh?)|(tanh?)"))
            {
                num1 = Convert.ToDouble(evalStack.Pop());
                switch (item)
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
                        break;
                }
            }
            else evalStack.Push(item);
        }
        Console.WriteLine($"evaluation: {evalStack.Peek()}");
        return Convert.ToDouble(evalStack.Pop());
    }
}
