using FluentAssertions;
using SolidLangCompiler.AST;
using SolidLangCompiler.AST.Types;
using SolidLangCompiler.SemanticAnalyzers;
using SolidLangCompiler.SemanticAnalyzers.Symbols;
using Xunit;

namespace SolidLangCompiler.UnitTests;

public class SymbolTableTests
{
    private static readonly TypeNode DummyType = new IntegerTypeNode(IntegerKind.I32);

    [Fact]
    public void SymbolTable_ShouldHaveGlobalScope()
    {
        var table = new SymbolTable();

        table.GlobalScope.Should().NotBeNull();
        table.CurrentScope.Should().Be(table.GlobalScope);
    }

    [Fact]
    public void SymbolTable_ShouldEnterAndExitScopes()
    {
        var table = new SymbolTable();
        var globalScope = table.GlobalScope;

        var funcScope = table.EnterScope("function");
        table.CurrentScope.Should().Be(funcScope);
        table.CurrentScope.Parent.Should().Be(globalScope);

        table.ExitScope();
        table.CurrentScope.Should().Be(globalScope);
    }

    [Fact]
    public void SymbolTable_ShouldNotExitGlobalScope()
    {
        var table = new SymbolTable();

        var act = () => table.ExitScope();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SymbolTable_ShouldCreateNestedScopes()
    {
        var table = new SymbolTable();

        var funcScope = table.EnterScope("function");
        var blockScope = table.EnterScope("block");

        table.CurrentScope.Should().Be(blockScope);
        table.CurrentScope.Parent.Should().Be(funcScope);

        table.ExitScope();
        table.CurrentScope.Should().Be(funcScope);

        table.ExitScope();
        table.CurrentScope.Should().Be(table.GlobalScope);
    }

    [Fact]
    public void Scope_ShouldAddSymbol()
    {
        var scope = new Scope("test");
        var symbol = new VariableSymbol("x", new SourceLocation("test.solid", 1, 1), DummyType);

        scope.AddSymbol(symbol);

        scope.Symbols.Should().ContainKey("x");
        scope.Symbols["x"].Should().Be(symbol);
    }

    [Fact]
    public void Scope_ShouldThrowOnDuplicateSymbol()
    {
        var scope = new Scope("test");
        var symbol1 = new VariableSymbol("x", new SourceLocation("test.solid", 1, 1), DummyType);
        var symbol2 = new VariableSymbol("x", new SourceLocation("test.solid", 2, 1), DummyType);

        scope.AddSymbol(symbol1);
        var act = () => scope.AddSymbol(symbol2);

        act.Should().Throw<SemanticException>()
            .WithMessage("*already defined*");
    }

    [Fact]
    public void Scope_TryAddSymbol_ShouldReturnFalseOnDuplicate()
    {
        var scope = new Scope("test");
        var symbol1 = new VariableSymbol("x", new SourceLocation("test.solid", 1, 1), DummyType);
        var symbol2 = new VariableSymbol("x", new SourceLocation("test.solid", 2, 1), DummyType);

        var result1 = scope.TryAddSymbol(symbol1);
        var result2 = scope.TryAddSymbol(symbol2);

        result1.Should().BeTrue();
        result2.Should().BeFalse();
    }

    [Fact]
    public void Scope_ShouldLookupSymbol()
    {
        var scope = new Scope("test");
        var symbol = new VariableSymbol("x", new SourceLocation("test.solid", 1, 1), DummyType);
        scope.AddSymbol(symbol);

        var found = scope.Lookup("x");

        found.Should().Be(symbol);
    }

    [Fact]
    public void Scope_ShouldLookupInParentScopes()
    {
        var globalScope = new Scope("global");
        var funcScope = new Scope("function", globalScope);

        var globalSymbol = new VariableSymbol("global_var", new SourceLocation("test.solid", 1, 1), DummyType);
        globalScope.AddSymbol(globalSymbol);

        var found = funcScope.Lookup("global_var");

        found.Should().Be(globalSymbol);
    }

    [Fact]
    public void Scope_ShouldReturnNullForUnknownSymbol()
    {
        var scope = new Scope("test");

        var found = scope.Lookup("unknown");

        found.Should().BeNull();
    }

    [Fact]
    public void Scope_LookupLocal_ShouldNotSearchParentScopes()
    {
        var globalScope = new Scope("global");
        var funcScope = new Scope("function", globalScope);

        var globalSymbol = new VariableSymbol("global_var", new SourceLocation("test.solid", 1, 1), DummyType);
        globalScope.AddSymbol(globalSymbol);

        var found = funcScope.LookupLocal("global_var");

        found.Should().BeNull();
    }

    [Fact]
    public void SymbolTable_ShouldAddSymbolToCurrentScope()
    {
        var table = new SymbolTable();
        var symbol = new VariableSymbol("x", new SourceLocation("test.solid", 1, 1), DummyType);

        table.AddSymbol(symbol);

        table.GlobalScope.Symbols.Should().ContainKey("x");
    }

    [Fact]
    public void SymbolTable_ShouldLookupFromCurrentScope()
    {
        var table = new SymbolTable();
        var symbol = new VariableSymbol("x", new SourceLocation("test.solid", 1, 1), DummyType);
        table.AddSymbol(symbol);

        var found = table.Lookup("x");

        found.Should().Be(symbol);
    }

    [Fact]
    public void SymbolTable_ShouldGetOrCreateNamespace()
    {
        var table = new SymbolTable();

        var ns1 = table.GetOrCreateNamespace("std");
        var ns2 = table.GetOrCreateNamespace("std");

        ns1.Should().BeSameAs(ns2);
        ns1.Parent.Should().Be(table.GlobalScope);
    }

    [Fact]
    public void SymbolTable_ShouldHandleDifferentSymbolTypes()
    {
        var scope = new Scope("test");

        var varSymbol = new VariableSymbol("x", new SourceLocation("test.solid", 1, 1), DummyType);
        var constSymbol = new ConstSymbol("PI", new SourceLocation("test.solid", 2, 1), DummyType);
        var funcSymbol = new FuncSymbol("main", new SourceLocation("test.solid", 3, 1));
        var typeSymbol = new TypeSymbol("MyInt", new SourceLocation("test.solid", 4, 1));

        scope.AddSymbol(varSymbol);
        scope.AddSymbol(constSymbol);
        scope.AddSymbol(funcSymbol);
        scope.AddSymbol(typeSymbol);

        scope.Lookup("x").Should().Be(varSymbol);
        scope.Lookup("PI").Should().Be(constSymbol);
        scope.Lookup("main").Should().Be(funcSymbol);
        scope.Lookup("MyInt").Should().Be(typeSymbol);
    }
}
