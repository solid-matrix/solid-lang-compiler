using SolidLang.Parser;
using SolidParser = SolidLang.Parser.Parser.Parser;

namespace SolidLang.Compiler;

class Program
{
    static int Main(string[] args)
    {
        // Test: Parse a simple Solid program with generics
        var testCode = """
            namespace test;

            using core::collections;

            struct Point {
                x: f32,
                y: f32,
            }

            struct Box<T> {
                value: T,
            }

            func add(a: i32, b: i32): i32 {
                return a + b;
            }

            func generic_func<T>(value: T): T where T: ICopy<T> {
                return value;
            }

            func test_generics() {
                var nested = List<Vector<f32>>{};
                var single = Box<i32>{value = 42};
            }
            """;

        Console.WriteLine("=== SolidLang Compiler Parser Test ===");
        Console.WriteLine();
        Console.WriteLine("Input:");
        Console.WriteLine(testCode);
        Console.WriteLine();

        var source = SourceText.From(testCode);
        
        var parser = new SolidParser(source);

        var program = parser.ParseProgram();

        Console.WriteLine("=== AST ===");
        Console.WriteLine(program.ToString());

        Console.WriteLine();
        Console.WriteLine("=== Diagnostics ===");
        if (parser.HasErrors)
        {
            foreach (var diag in parser.Diagnostics)
            {
                Console.WriteLine(diag);
            }
            return 1;
        }
        else
        {
            Console.WriteLine("No errors.");
        }

        return 0;
    }
}
