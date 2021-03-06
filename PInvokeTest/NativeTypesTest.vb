﻿' Copyright (c) Microsoft Corporation.  All rights reserved.
'The following code was generated by Microsoft Visual Studio 2005.
'The test owner should check each test for validity.
Imports System
Imports System.Text
Imports System.Collections.Generic
Imports PInvoke
Imports PInvoke.Parser
Imports Xunit

'''<summary>
'''This is a test class for PInvoke.Parser.NativeType and is intended
'''to contain all PInvoke.Parser.NativeType Unit Tests
'''</summary>
Public Class NativeSymbolTest

    Private Sub VerifyReachable(ByVal nt As NativeType, ByVal ParamArray names As String())
        Dim bag As New NativeSymbolBag()

        Dim definedNt As NativeDefinedType = TryCast(nt, NativeDefinedType)
        Dim typedefNt As NativeTypeDef = TryCast(nt, NativeTypeDef)
        If definedNt IsNot Nothing Then
            bag.AddDefinedType(DirectCast(nt, NativeDefinedType))
        ElseIf typedefNt IsNot Nothing Then
            bag.AddTypedef(DirectCast(nt, NativeTypeDef))
        Else
            Throw New Exception("Not a searchable type")
        End If

        VerifyReachable(bag, names)
    End Sub

    Private Sub VerifyReachable(ByVal bag As NativeSymbolBag, ByVal ParamArray names As String())
        Assert.NotNull(bag)
        Dim map As New Dictionary(Of String, NativeType)
        For Each curSym As NativeSymbol In bag.FindAllReachableNativeSymbols()
            Dim cur As NativeType = TryCast(curSym, NativeType)
            If cur IsNot Nothing Then
                Dim nt As NativeType = TryCast(cur, NativeType)
                If nt IsNot Nothing Then
                    map.Add(cur.DisplayName, nt)
                End If
            End If
        Next

        Assert.Equal(names.Length, map.Count)
        For Each name As String In names
            Assert.True(map.ContainsKey(name), "NativeType with name " & name & " not reachable")
        Next

    End Sub

    Private Function Print(ByVal ns As NativeSymbol) As String
        If ns Is Nothing Then
            Return "<Nothing>"
        End If

        Dim str As String = ns.Name
        For Each child As NativeSymbol In ns.GetChildren()
            str &= "(" & Print(child) & ")"
        Next

        Return str
    End Function

    Private Sub VerifyTree(ByVal ns As NativeSymbol, ByVal str As String)
        Dim realStr As String = Print(ns)
        Assert.Equal(str, realStr)
    End Sub

    ''' <summary>
    ''' simple test with no children
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Iterate1()
        Dim nt1 As New NativeStruct()
        nt1.Name = "s1"
        VerifyReachable(nt1, "s1")
    End Sub

    <Fact>
    Public Sub Iterate2()
        Dim nt1 As New NativeStruct()
        nt1.Name = "s1"
        nt1.Members.Add(New NativeMember("f", New NativeBuiltinType(BuiltinType.NativeInt32)))
        VerifyReachable(nt1, "s1", "int")
    End Sub


    ''' <summary>
    ''' Simple test with a couple of structs
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Iterate3()
        Dim nt1 As New NativeStruct()
        nt1.Name = "s1"

        Dim nt2 As New NativeStruct()
        nt2.Name = "s2"

        Dim bag As New NativeSymbolBag()
        bag.AddDefinedType(nt1)
        bag.AddDefinedType(nt2)
        VerifyReachable(bag, "s1", "s2")
    End Sub

    ''' <summary>
    ''' Test a simple proxy type
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Iterate4()
        Dim nt1 As New NativeTypeDef("td1")
        Dim nt2 As New NativeNamedType("n1")
        nt1.RealType = nt2
        VerifyReachable(nt1, "td1", "n1")
    End Sub

    ''' <summary>
    ''' Proxy within a container
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Iterate5()
        Dim nt1 As New NativeStruct("s1")
        Dim nt2 As New NativeTypeDef("td1")
        Dim nt3 As New NativeNamedType("n1")
        nt2.RealType = nt3
        nt1.Members.Add(New NativeMember("foo", nt2))
        VerifyReachable(nt1, "s1", "td1", "n1")
    End Sub

    ''' <summary>
    ''' Play around with structs
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Child1()
        Dim s1 As New NativeStruct("s1")
        VerifyTree(s1, "s1")
        s1.Members.Add(New NativeMember("m1", New NativeBuiltinType(BuiltinType.NativeChar)))
        VerifyTree(s1, "s1(m1(char))")
        s1.Members.Add(New NativeMember("m2", New NativeBuiltinType(BuiltinType.NativeByte)))
        VerifyTree(s1, "s1(m1(char))(m2(byte))")
    End Sub

    ''' <summary>
    ''' Replace the children of a struct
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Child2()
        Dim s1 As New NativeStruct("s1")
        s1.Members.Add(New NativeMember("m1", New NativeBuiltinType(BuiltinType.NativeByte)))
        s1.ReplaceChild(s1.Members(0), New NativeMember("m2", New NativeBuiltinType(BuiltinType.NativeDouble)))
        VerifyTree(s1, "s1(m2(double))")
    End Sub

    ''' <summary>
    ''' Children of an enumeration
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Child3()
        Dim e1 As New NativeEnum("e1")
        e1.Values.Add(New NativeEnumValue("n1", "v1"))
        VerifyTree(e1, "e1(n1(Value(v1)))")
        e1.Values.Add(New NativeEnumValue("n2", "v2"))
        VerifyTree(e1, "e1(n1(Value(v1)))(n2(Value(v2)))")

    End Sub

    ''' <summary>
    ''' adding a member to an enum shouldn't be part of the enumeration
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Child4()
        Dim e1 As New NativeEnum("e1")
        e1.Members.Add(New NativeMember("m1", New NativeBuiltinType(BuiltinType.NativeByte)))
        VerifyTree(e1, "e1")
    End Sub

    ''' <summary>
    ''' Verify an enum replace 
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Child5()
        Dim e1 As New NativeEnum("e1")
        e1.Values.Add(New NativeEnumValue("n1", "v1"))
        e1.ReplaceChild(e1.Values(0), New NativeEnumValue("n2", "v2"))
        VerifyTree(e1, "e1(n2(Value(v2)))")
    End Sub

    ''' <summary>
    ''' Verify a proc
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Child6()
        Dim proc As New NativeProcedure("proc")
        proc.Signature.ReturnType = New NativeBuiltinType(BuiltinType.NativeByte)
        VerifyTree(proc, "proc(Sig(byte)(Sal))")
        proc.Signature.Parameters.Add(New NativeParameter("p1", New NativeBuiltinType(BuiltinType.NativeChar)))
        VerifyTree(proc, "proc(Sig(byte)(Sal)(p1(char)(Sal)))")
    End Sub

    ''' <summary>
    ''' Replace the parameters of a proc
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Child7()
        Dim proc As New NativeProcedure("proc")
        proc.Signature.ReturnType = New NativeBuiltinType(BuiltinType.NativeByte)
        proc.Signature.Parameters.Add(New NativeParameter("p1", New NativeBuiltinType(BuiltinType.NativeChar)))
        proc.Signature.ReplaceChild(proc.Signature.ReturnType, New NativeBuiltinType(BuiltinType.NativeFloat))
        VerifyTree(proc, "proc(Sig(float)(Sal)(p1(char)(Sal)))")
        proc.Signature.ReplaceChild(proc.Signature.Parameters(0), New NativeParameter("p2", New NativeBuiltinType(BuiltinType.NativeChar)))
        VerifyTree(proc, "proc(Sig(float)(Sal)(p2(char)(Sal)))")
    End Sub

End Class

Public Class NativeBuiltinTypeTest

    <Fact>
    Public Sub TestAll()
        For Each bt As BuiltinType In System.Enum.GetValues(GetType(BuiltinType))
            If bt <> BuiltinType.NativeUnknown Then
                Dim nt As New NativeBuiltinType(bt)
                Assert.Equal(nt.Name, NativeBuiltinType.BuiltinTypeToName(bt))
                Assert.Equal(bt, nt.BuiltinType)
                Assert.NotNull(nt.ManagedType)
                Assert.NotEqual(0, CInt(nt.UnmanagedType))
            End If
        Next
    End Sub

    ''' <summary>
    ''' Unknown type is used to handle situations where we just don't know what's going
    ''' on so we add the unknown type.  Typically meant for use by third parties
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Unknown1()
        Dim nt As New NativeBuiltinType("foo")
        Assert.Equal(BuiltinType.NativeUnknown, nt.BuiltinType)
        Assert.Equal("unknown", nt.Name)
        Assert.Equal("unknown", nt.DisplayName)
    End Sub

    <Fact>
    Public Sub EnsureTypeKeywordToBuiltin()
        For Each cur As TokenType In EnumUtil.GetAllValues(Of TokenType)()
            If TokenHelper.IsTypeKeyword(cur) Then
                Dim bt As NativeBuiltinType = Nothing
                Assert.True(NativeBuiltinType.TryConvertToBuiltinType(cur, bt))
            End If
        Next
    End Sub


End Class

Public Class NativeProxyTypeTest

    ''' <summary>
    ''' Basic typedef cases
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Dig1()
        Dim td As New NativeTypeDef("foo")
        td.RealType = New NativeBuiltinType(BuiltinType.NativeByte)
        Assert.Same(td.RealType, td.DigThroughTypedefAndNamedTypes())

        Dim outerTd As New NativeTypeDef("bar")
        outerTd.RealType = td
        Assert.Same(td.RealType, outerTd.DigThroughTypedefAndNamedTypes())
    End Sub

    ''' <summary>
    ''' Simple tests with named types
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Dig2()
        Dim named As New NativeNamedType("foo")
        named.RealType = New NativeBuiltinType(BuiltinType.NativeByte)
        Assert.Same(named.RealType, named.DigThroughTypedefAndNamedTypes())

        Dim outerNamed As New NativeNamedType("bar")
        outerNamed.RealType = named
        Assert.Same(named.RealType, outerNamed.DigThroughTypedefAndNamedTypes())
    End Sub

    ''' <summary>
    ''' Hit some null blocks
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Dig3()
        Dim named As New NativeNamedType("foo")
        Assert.Null(named.DigThroughTypedefAndNamedTypes())
    End Sub

    ''' <summary>
    ''' Don't dig through pointers
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Dig4()
        Dim pt As New NativePointer(BuiltinType.NativeByte)
        Assert.Same(pt, pt.DigThroughTypedefAndNamedTypes())

        Dim td As New NativeTypeDef("foo", pt)
        Assert.Same(pt, td.DigThroughTypedefAndNamedTypes())
    End Sub

    ''' <summary>
    ''' Dig and search
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Dig5()
        Dim pt1 As New NativePointer(New NativeTypeDef("foo", BuiltinType.NativeFloat))
        Assert.Equal(NativeSymbolKind.BuiltinType, pt1.RealType.DigThroughTypedefAndNamedTypes().Kind)
        Assert.Equal(NativeSymbolKind.TypedefType, pt1.RealType.DigThroughTypedefAndNamedTypesFor("foo").Kind)
        Assert.Null(pt1.RealType.DigThroughTypedefAndNamedTypesFor("bar"))
    End Sub

    ''' <summary>
    ''' Dig and search again
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Dig6()
        Dim named As New NativeNamedType("bar", New NativeTypeDef("td1", BuiltinType.NativeFloat))
        Dim td As New NativeTypeDef("foo", named)

        Assert.Equal(NativeSymbolKind.TypedefType, td.DigThroughTypedefAndNamedTypesFor("foo").Kind)
        Assert.Same(td, td.DigThroughTypedefAndNamedTypesFor("foo"))
        Assert.Equal(NativeSymbolKind.BuiltinType, td.DigThroughTypedefAndNamedTypes().Kind)
        Assert.Equal(NativeSymbolKind.NamedType, td.DigThroughTypedefAndNamedTypesFor("bar").Kind)

        Dim named2 As New NativeNamedType("named2", td)
        Assert.Equal(NativeSymbolKind.TypedefType, named2.DigThroughNamedTypesFor("foo").Kind)
        Assert.Null(named2.DigThroughNamedTypesFor("bar"))
    End Sub

    ''' <summary>
    ''' Parameters should have sal attributes
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Sal1()
        Dim param As New NativeParameter()
        Assert.NotNull(param.SalAttribute)
    End Sub

    ''' <summary>
    ''' The return type should have a sal attribute by default
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Sal2()
        Dim proc As New NativeProcedure()
        Assert.NotNull(proc.Signature.ReturnTypeSalAttribute)
    End Sub

    ''' <summary>
    ''' Make sure each sal entry has a directive
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Sal3()
        For Each e As SalEntryType In System.Enum.GetValues(GetType(SalEntryType))
            Assert.False(String.IsNullOrEmpty(NativeSalEntry.GetDirectiveForEntry(SalEntryType.ElemReadableTo)))
        Next
    End Sub
End Class

Public Class NativeParameterTest

    <Fact>
    Public Sub Pre()
        Dim param As New NativeParameter()
        Assert.NotNull(param.Name)
    End Sub

    ''' <summary>
    ''' To be resolved we only need a type.  Function pointer parameters don't have to have names
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Resolved()
        Dim param As New NativeParameter()
        Assert.False(param.IsImmediateResolved)
        param.NativeType = New NativeBuiltinType(BuiltinType.NativeByte)
        Assert.True(param.IsImmediateResolved)
        param.Name = "foo"
        Assert.True(param.IsImmediateResolved)
    End Sub
End Class

Public Class NativeProcedureTest

    <Fact>
    Public Sub Pre()
        Dim proc As New NativeProcedure()
        Assert.NotNull(proc.Signature)
    End Sub

End Class

Public Class NativeFunctionPointerTest

    <Fact>
    Public Sub Pre()
        Dim ptr As New NativeFunctionPointer("foo")
        Assert.NotNull(ptr.Signature)
    End Sub

End Class

Public Class NativeValueExpressionTest

    <Fact>
    Public Sub Value1()
        Dim expr As New NativeValueExpression("1+1")
        Assert.Equal(2, expr.Values.Count)
        Assert.Equal(NativeValueKind.Number, expr.Values(0).ValueKind)
        Assert.Equal("1", expr.Values(0).DisplayValue)
        Assert.Equal(1, CInt(expr.Values(0).Value))
    End Sub

    <Fact>
    Public Sub Value2()
        Dim expr As New NativeValueExpression("FOO+1")
        Assert.Equal(2, expr.Values.Count)
        Assert.Equal("FOO", expr.Values(0).DisplayValue)
        Assert.Equal("FOO", expr.Values(0).Name)
        Assert.Null(expr.Values(0).SymbolValue)
    End Sub

    <Fact>
    Public Sub Value3()
        Dim expr As New NativeValueExpression("FOO+BAR")
        Assert.Equal(2, expr.Values.Count)
        Assert.Equal("FOO", expr.Values(0).DisplayValue)
        Assert.Equal("BAR", expr.Values(1).DisplayValue)
    End Sub

    <Fact>
    Public Sub Value4()
        Dim expr As New NativeValueExpression("""bar""+1")
        Assert.Equal(2, expr.Values.Count)
        Assert.Equal(NativeValueKind.String, expr.Values(0).ValueKind)
        Assert.Equal("bar", expr.Values(0).DisplayValue)
    End Sub

    ''' <summary>
    ''' Test the parsing of cast operations
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Value5()
        Dim expr As New NativeValueExpression("(DWORD)5")
        Assert.Equal(2, expr.Values.Count)

        Dim val As NativeValue = expr.Values(0)
        Assert.Equal(NativeValueKind.SymbolType, val.ValueKind)
        Assert.Equal("DWORD", val.DisplayValue)

        val = expr.Values(1)
        Assert.Equal(NativeValueKind.Number, val.ValueKind)
        Assert.Equal(5, CInt(val.Value))
    End Sub

    <Fact>
    Public Sub Value6()
        Dim expr As New NativeValueExpression("(DWORD)(5+6)")
        Assert.Equal(3, expr.Values.Count)

        Dim val As NativeValue = expr.Values(0)
        Assert.Equal(NativeValueKind.SymbolType, val.ValueKind)
        Assert.Equal("DWORD", val.DisplayValue)

        val = expr.Values(1)
        Assert.Equal(NativeValueKind.Number, val.ValueKind)
        Assert.Equal(5, CInt(val.Value))

        val = expr.Values(2)
        Assert.Equal(NativeValueKind.Number, val.ValueKind)
        Assert.Equal(6, CInt(val.Value))
    End Sub

    ''' <summary>
    ''' Make sure than bad value expressions are marked as resolvable
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub BadValue1()
        Dim expr As New NativeValueExpression("&&&")
        Assert.True(expr.IsImmediateResolved)
        Assert.False(expr.IsParsable)
    End Sub

    ''' <summary>
    ''' Reseting the value should cause a re-parse 
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub BadValue2()
        Dim expr As New NativeValueExpression("&&&")
        Assert.True(expr.IsImmediateResolved)
        Assert.False(expr.IsParsable)
        Assert.Equal(0, expr.Values.Count)
        expr.Expression = "1+1"
        Assert.True(expr.IsImmediateResolved)
        Assert.True(expr.IsParsable)
        Assert.Equal(2, expr.Values.Count)
    End Sub

End Class

Public Class NativeValueTest

    <Fact>
    Public Sub Resolve1()
        Dim val As NativeValue = NativeValue.CreateNumber(1)
        Assert.Equal(1, CInt(val.Value))
        Assert.Equal(NativeValueKind.Number, val.ValueKind)
        Assert.True(val.IsImmediateResolved)
    End Sub

    <Fact>
    Public Sub Resolve2()
        Dim val As NativeValue = NativeValue.CreateString("foo")
        Assert.Equal("foo", CStr(val.Value))
        Assert.Equal(NativeValueKind.String, val.ValueKind)
        Assert.True(val.IsImmediateResolved)
    End Sub

    <Fact>
    Public Sub Resolve3()
        Dim val As NativeValue = NativeValue.CreateSymbolType("foo")
        Assert.Equal("foo", val.Name)
        Assert.Equal(NativeValueKind.SymbolType, val.ValueKind)
        Assert.True(val.IsImmediateResolved)
        val.Value = New NativeBuiltinType(BuiltinType.NativeByte)
        Assert.True(val.IsImmediateResolved)
    End Sub

    <Fact>
    Public Sub Resolve4()
        Dim val As NativeValue = NativeValue.CreateSymbolValue("bar")
        Assert.Equal("bar", val.Name)
        Assert.Equal(NativeValueKind.SymbolValue, val.ValueKind)
        Assert.True(val.IsImmediateResolved)
        val.Value = New NativeBuiltinType(BuiltinType.NativeByte)
        Assert.True(val.IsImmediateResolved)
    End Sub

    <Fact>
    Public Sub Resolve5()
        Dim val As NativeValue = NativeValue.CreateSymbolValue("foo", New NativeBuiltinType(BuiltinType.NativeBoolean))
        Assert.Equal("foo", val.Name)
        Assert.Equal(NativeValueKind.SymbolValue, val.ValueKind)
        Assert.NotNull(val.SymbolValue)
        Assert.Null(val.SymbolType)
        Assert.True(val.IsImmediateResolved)
    End Sub

    <Fact>
    Public Sub Resolve6()
        Dim val As NativeValue = NativeValue.CreateSymbolType("foo", New NativeBuiltinType(BuiltinType.NativeBoolean))
        Assert.Equal("foo", val.Name)
        Assert.Equal(NativeValueKind.SymbolType, val.ValueKind)
        Assert.Null(val.SymbolValue)
        Assert.NotNull(val.SymbolType)
        Assert.True(val.IsImmediateResolved)
    End Sub

    <Fact>
    Public Sub Resolve7()
        Dim val As NativeValue = NativeValue.CreateCharacter("c"c)
        Assert.Equal("c"c, CStr(val.Value))
        Assert.Equal(NativeValueKind.Character, val.ValueKind)
        Assert.True(val.IsImmediateResolved)
    End Sub

    ''' <summary>
    ''' Value should not update the enumeration 
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Dynamic1()
        Dim val As NativeValue = NativeValue.CreateNumber(1)
        Assert.Equal(NativeValueKind.Number, val.ValueKind)
        val.Value = 42
        Assert.Equal(NativeValueKind.Number, val.ValueKind)
        Assert.Equal(42, CInt(val.Value))
    End Sub

    ''' <summary>
    ''' Changing the type should not update the kind
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Dynamic2()
        Dim val As NativeValue = NativeValue.CreateNumber(42)
        Assert.Equal(NativeValueKind.Number, val.ValueKind)
        val.Value = "foo"
        Assert.Equal(NativeValueKind.Number, val.ValueKind)
        Assert.Equal("foo", CStr(val.Value))
    End Sub

    <Fact>
    Public Sub IsValueResolved1()
        Dim val As NativeValue = NativeValue.CreateBoolean(True)
        Assert.True(val.IsValueResolved)
        val.Value = Nothing
        Assert.False(val.IsValueResolved)
    End Sub

    <Fact>
    Public Sub IsValueResolved2()
        Dim val As NativeValue = NativeValue.CreateCharacter("c"c)
        Assert.True(val.IsValueResolved)
        val.Value = Nothing
        Assert.False(val.IsValueResolved)
    End Sub

    <Fact>
    Public Sub IsValueResolved3()
        Dim val As NativeValue = NativeValue.CreateNumber(42)
        Assert.True(val.IsValueResolved)
        val.Value = Nothing
        Assert.False(val.IsValueResolved)
        val.Value = 42
        Assert.True(val.IsValueResolved)
    End Sub

    <Fact>
    Public Sub IsValueResolved4()
        Dim val As NativeValue = NativeValue.CreateSymbolType("foo")
        Assert.False(val.IsValueResolved)
        val.Value = New NativeStruct("foo")
        Assert.True(val.IsValueResolved)
        val.Value = Nothing
        Assert.False(val.IsValueResolved)
    End Sub

    <Fact>
    Public Sub IsValueResolved5()
        Dim val As NativeValue = NativeValue.CreateSymbolValue("foo")
        Assert.False(val.IsValueResolved)
        val.Value = New NativeStruct("foo")
        Assert.True(val.IsValueResolved)
        val.Value = Nothing
        Assert.False(val.IsValueResolved)
    End Sub

End Class

Public Class NativeConstantTest

    <Fact>
    Public Sub Empty()
        Dim c1 As New NativeConstant("c1")
        Assert.Equal(ConstantKind.Macro, c1.ConstantKind)
        Assert.Equal("c1", c1.Name)
    End Sub

    <Fact>
    Public Sub Value1()
        Dim c1 As New NativeConstant("p", "1+2")
        Assert.Equal(ConstantKind.Macro, c1.ConstantKind)
        Assert.Equal("1+2", c1.Value.Expression)
        Assert.Equal("p", c1.Name)
    End Sub

    ''' <summary>
    ''' Make sure that we quote macro method values to ensure that they
    ''' are "resolvable"
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Method1()
        Dim sig As String = "(x) x+1"
        Dim c1 As New NativeConstant("c1", sig, ConstantKind.MacroMethod)
        Assert.Equal(ConstantKind.MacroMethod, c1.ConstantKind)
        Assert.Equal("""" & sig & """", c1.Value.Expression)
        Assert.Equal("c1", c1.Name)
    End Sub
End Class
