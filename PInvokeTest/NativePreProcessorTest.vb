﻿' Copyright (c) Microsoft Corporation.  All rights reserved.
'The following code was generated by Microsoft Visual Studio 2005.
'The test owner should check each test for validity.
Imports System
Imports System.Text
Imports System.Collections.Generic
Imports PInvoke.Parser
Imports System.IO
Imports Xunit

'''<summary>
'''This is a test class for PInvoke.Parser.NativePreProcessor and is intended
'''to contain all PInvoke.Parser.NativePreProcessor Unit Tests
'''</summary>
Public Class PreProcessorEngineTest

    Private Sub VerifyCount(ByVal text As String, ByVal errorCount As Integer, ByVal warningCount As Integer)
        Dim p As New PreProcessorEngine(New PreProcessorOptions)
        p.Process(New TextReaderBag("text", New StringReader(text)))
        Assert.Equal(errorCount, p.ErrorProvider.Errors.Count)
        Assert.Equal(warningCount, p.ErrorProvider.Warnings.Count)
    End Sub

    Private Function VerifyImpl(ByVal opts As PreProcessorOptions, ByVal before As String, ByVal after As String) As Dictionary(Of String, Macro)
        Dim p As New PreProcessorEngine(opts)
        Dim actual As String = p.Process(New TextReaderBag("before", New StringReader(before)))
        Assert.Equal(after, actual)

        Return p.MacroMap
    End Function

    Private Function VerifyNormal(ByVal before As String, ByVal after As String) As Dictionary(Of String, Macro)
        Return VerifyNormal(New List(Of Macro), before, after)
    End Function

    Private Function VerifyNormal(ByVal initialList As List(Of Macro), ByVal before As String, ByVal after As String) As Dictionary(Of String, Macro)
        Dim opts As New PreProcessorOptions()
        opts.InitialMacroList.AddRange(initialList)
        opts.FollowIncludes = False
        Return VerifyImpl(opts, before, after)
    End Function

    Private Function VerifyNoMetadata(ByVal before As String, ByVal after As String) As Dictionary(Of String, Macro)
        Dim opts As New PreProcessorOptions()
        opts.FollowIncludes = True
        Return VerifyImpl(opts, before, after)
    End Function

    Private Sub VerifyMap(ByVal map As Dictionary(Of String, Macro), ByVal ParamArray args() As String)
        For i As Integer = 0 To args.Length - 1 Step 2
            Dim name As String = args(i)
            Dim value As String = args(i + 1)
            Assert.True(map.ContainsKey(name), "Could not find " & name & " in the macro map")

            Dim macro As Macro = map(name)
            Assert.Equal(value, macro.Value)
        Next
    End Sub

    Private Function VerifyMacro(ByVal data As String, ByVal ParamArray args() As String) As Dictionary(Of String, Macro)
        Dim opts As New PreProcessorOptions()
        opts.FollowIncludes = False
        Dim p As New PreProcessorEngine(opts)
        p.Process(New TextReaderBag("foo", New StringReader(data)))
        VerifyMap(p.MacroMap, args)
        Return p.MacroMap
    End Function

    Private Function EvalCond(ByVal list As List(Of Macro), ByVal cond As String) As Boolean
        Dim before As String =
            "#if " & cond & vbCrLf &
            "true" & vbCrLf &
            "#else" & vbCrLf &
            "false" & vbCrLf &
            "#endif"
        Dim opts As New PreProcessorOptions()
        opts.InitialMacroList.AddRange(list)
        Dim engine As New PreProcessorEngine(opts)
        Dim val As String = engine.Process(New TextReaderBag("foo", New StringReader(before)))
        Return val.StartsWith("true")
    End Function

    Private Sub VerifyCondTrue(ByVal list As List(Of Macro), ByVal cond As String)
        Assert.True(EvalCond(list, cond))
    End Sub

    Private Sub VerifyCondFalse(ByVal list As List(Of Macro), ByVal cond As String)
        Assert.False(EvalCond(list, cond))
    End Sub

    <Fact>
    Public Sub Conditional1()
        Dim before As String =
            "#define foo bar" & vbCrLf &
            "#if foo" & vbCrLf &
            "hello" & vbCrLf &
            "#endif"
        Dim after As String = "hello" & vbCrLf
        Dim map As Dictionary(Of String, Macro)
        map = VerifyNormal(before, after)
        VerifyMap(map, "foo", "bar")
    End Sub

    <Fact>
    Public Sub Conditional2()
        Dim before As String =
            "#define foo bar" & vbCrLf &
            "#if foo" & vbCrLf &
            "hello" & vbCrLf &
            "world" & vbCrLf &
            "#endif"
        Dim after As String =
            "hello" & vbCrLf &
            "world" & vbCrLf
        Dim map As Dictionary(Of String, Macro)
        map = VerifyNormal(before, after)
        VerifyMap(map, "foo", "bar")
    End Sub

    <Fact>
    Public Sub Conditional3()
        Dim before As String =
            "#define foo bar" & vbCrLf &
            "#if foo" & vbCrLf &
            "hello" & vbCrLf &
            "#else" & vbCrLf &
            "world" & vbCrLf &
            "#endif"
        Dim after As String =
            "hello" & vbCrLf
        Dim map As Dictionary(Of String, Macro)
        map = VerifyNormal(before, after)
        VerifyMap(map, "foo", "bar")
    End Sub

    ''' <summary>
    ''' Hit the else clause
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Conditional4()
        Dim before As String =
            "#if foo" & vbCrLf &
            "hello" & vbCrLf &
            "#else" & vbCrLf &
            "world" & vbCrLf &
            "#endif"
        Dim after As String =
            "world" & vbCrLf
        Dim map As Dictionary(Of String, Macro)
        map = VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' Hit the #elseif
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Conditional5()
        Dim before As String =
            "#define foo bar" & vbCrLf &
            "#if bar" & vbCrLf &
            "hello" & vbCrLf &
            "#elseif foo" & vbCrLf &
            "world" & vbCrLf &
            "#endif"
        Dim after As String =
            "world" & vbCrLf
        Dim map As Dictionary(Of String, Macro)
        map = VerifyNormal(before, after)
        VerifyMap(map, "foo", "bar")
    End Sub

    ''' <summary>
    ''' Skip the else when the #elseif is hit
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Conditional6()
        Dim before As String =
            "#define foo bar" & vbCrLf &
            "#if bar " & vbCrLf &
            "hello" & vbCrLf &
            "#elseif foo" & vbCrLf &
            "world" & vbCrLf &
            "#else" & vbCrLf &
            "again" & vbCrLf &
            "#endif"
        Dim after As String =
            "world" & vbCrLf
        Dim map As Dictionary(Of String, Macro)
        map = VerifyNormal(before, after)
        VerifyMap(map, "foo", "bar")
    End Sub

    <Fact>
    Public Sub Conditional7()
        Dim before As String =
            "#define _PREFAST_" & vbCrLf &
            "#if !(defined(_midl)) && defined(_PREFAST_)" & vbCrLf &
            "hello" & vbCrLf &
            "#endif"
        Dim after As String =
            "hello" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' make sure that we collapse #     define
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Conditional8()
        Dim before As String =
                  "#     define foo bar" & vbCrLf &
                  "#     if bar" & vbCrLf &
                  "hello" & vbCrLf &
                  "#    elseif foo" & vbCrLf &
                  "world" & vbCrLf &
                  "#    endif"
        Dim after As String =
            "world" & vbCrLf
        Dim map As Dictionary(Of String, Macro)
        map = VerifyNormal(before, after)
        VerifyMap(map, "foo", "bar")
    End Sub

    <Fact>
    Public Sub Conditional9()
        Dim before As String =
                  "#     define foo bar" & vbCrLf &
                  "#     if defined foo " & vbCrLf &
                  "hello" & vbCrLf &
                  "#    else " & vbCrLf &
                  "world" & vbCrLf &
                  "#    endif"
        Dim after As String =
            "hello" & vbCrLf
        Dim map As Dictionary(Of String, Macro)
        map = VerifyNormal(before, after)
        VerifyMap(map, "foo", "bar")
    End Sub

    <Fact>
    Public Sub Conditional10()
        Dim before As String =
            "#define FOO 1" & vbCrLf &
            "#if FOO & 1" & vbCrLf &
            "hello" & vbCrLf &
            "#endif"
        Dim after As String =
            "hello" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    <Fact>
    Public Sub Conditional11()
        Dim before As String =
            "#define FOO 1" & vbCrLf &
            "#if FOO & 2" & vbCrLf &
            "hello" & vbCrLf &
            "#endif"
        Dim after As String = String.Empty
        VerifyNormal(before, after)
    End Sub

    <Fact>
    Public Sub Conditional12()
        Dim before As String =
            "#define FOO 1" & vbCrLf &
            "#if FOO | 2" & vbCrLf &
            "hello" & vbCrLf &
            "#endif"
        Dim after As String =
            "hello" & vbCrLf
        VerifyNormal(before, after)
    End Sub


    ''' <summary>
    ''' Simple multiline macro.  
    ''' 
    ''' The hello line will end with a newline because it's the last line in the file.
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Multiline1()
        Dim before As String =
            "#define foo bar \" & vbCrLf &
            "baz" & vbCrLf &
            "hello"
        Dim after As String =
            "hello" & vbCrLf
        Dim map As Dictionary(Of String, Macro)
        map = VerifyNormal(before, after)
        VerifyMap(map, "foo", "bar baz")
    End Sub


    <Fact>
    Public Sub Multiline2()
        Dim before As String =
            "#define foo bar \" & vbCrLf &
            "baz \" & vbCrLf &
            "again " & vbCrLf &
            "hello"
        Dim after As String =
            "hello" & vbCrLf
        Dim map As Dictionary(Of String, Macro)
        map = VerifyNormal(before, after)
        VerifyMap(map, "foo", "bar baz again")
    End Sub

    <Fact>
    Public Sub Multiline3()
        Dim before As String =
            "#define foo bar \" & vbCrLf &
            "baz /*foo*/\" & vbCrLf &
            " " & vbCrLf &
            "hello"
        Dim after As String =
            "hello" & vbCrLf
        Dim map As Dictionary(Of String, Macro)
        map = VerifyNormal(before, after)
        VerifyMap(map, "foo", "bar baz")
    End Sub

    <Fact>
    Public Sub Multiline4()
        Dim before As String =
            "#define foo bar \" & vbCrLf &
            "baz /*foo*/\" & vbCrLf &
            " // hello " & vbCrLf &
            "hello"
        Dim after As String =
            "hello" & vbCrLf
        Dim map As Dictionary(Of String, Macro)
        map = VerifyNormal(before, after)
        VerifyMap(map, "foo", "bar baz")
    End Sub

    ''' <summary>
    ''' Preprocessor should remove comments
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Comment1()
        Dim before As String =
            "/* hello */" & vbCrLf &
            "world" & vbCrLf
        Dim after As String =
            vbCrLf & "world" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    <Fact>
    Public Sub Comment2()
        Dim before As String =
            "hello" & vbCrLf &
            "/* hello */" & vbCrLf &
            "world" & vbCrLf
        Dim after As String =
            "hello" & vbCrLf &
            vbCrLf &
            "world" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    <Fact>
    Public Sub Comment3()
        Dim before As String =
            "// hello */" & vbCrLf &
            "world" & vbCrLf
        Dim after As String =
            vbCrLf & "world" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' Make sure the preprocessor won't ignore an entire line when it hits
    ''' a comment
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Comment4()
        Dim before As String =
            "/* hello */ hello" & vbCrLf &
            "world" & vbCrLf
        Dim after As String =
            " hello" & vbCrLf &
            "world" & vbCrLf
        VerifyNormal(before, after)
    End Sub
    ''' <summary>
    ''' Parse out simple macros
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Macro1()
        Dim data As String =
            "#define foo bar" & vbCrLf &
            "#define bar foo"
        VerifyMacro(data, "foo", "bar", "bar", "foo")
    End Sub

    ''' <summary>
    ''' Comment in the value
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Macro2()
        Dim data As String =
            "#define foo /* hello */bar" & vbCrLf &
            "#define bar foo"
        VerifyMacro(data, "foo", "bar", "bar", "foo")
    End Sub

    ''' <summary>
    ''' Undef the macro
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Macro3()
        Dim data As String =
            "#define foo bar" & vbCrLf &
            "#undef foo"
        Dim map As Dictionary(Of String, Macro) = VerifyMacro(data)
        Assert.Equal(0, map.Count)
    End Sub

    <Fact>
    Public Sub Macro4()
        Dim data As String =
            "#define foo bar" & vbCrLf &
            "#define /* hollow */ bar foo"
        VerifyMacro(data, "foo", "bar", "bar", "foo")
    End Sub

    ''' <summary>
    ''' Make sure that if the values are just wrapped in a set of () that we don't 
    ''' treat it as a macro method
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Macro5()
        Dim before As String =
            "#define foo   (1)" & vbCrLf &
            "foo" & vbCrLf
        Dim after As String =
            "(1)" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    <Fact>
    Public Sub Macro6()
        Dim before As String =
            "#define foo   ((2)1)" & vbCrLf &
            "foo" & vbCrLf
        Dim after As String =
            "((2)1)" & vbCrLf
        VerifyNormal(before, after)
    End Sub


    <Fact>
    Public Sub Eval1()
        Dim list As New List(Of Macro)
        list.Add(New Macro("foo", "bar"))
        VerifyCondTrue(list, "foo")
    End Sub

    <Fact>
    Public Sub Eval2()
        Dim list As New List(Of Macro)
        list.Add(New Macro("m1", "true"))
        list.Add(New Macro("m2", "false"))
        list.Add(New Macro("m3", "1"))
        list.Add(New Macro("m4", "0"))
        VerifyCondFalse(list, "foo")
    End Sub

    ''' <summary>
    ''' Test the "defined" function
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Eval3()
        Dim list As New List(Of Macro)
        list.Add(New Macro("m1", "true"))
        list.Add(New Macro("m2", "false"))
        list.Add(New Macro("m3", "1"))
        list.Add(New Macro("m4", "0"))
        VerifyCondTrue(list, "defined(m1)")
    End Sub

    ''' <summary>
    ''' Add some parens
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Eval4()
        Dim list As New List(Of Macro)
        list.Add(New Macro("m1", "true"))
        list.Add(New Macro("m2", "false"))
        list.Add(New Macro("m3", "1"))
        list.Add(New Macro("m4", "0"))
        VerifyCondTrue(list, "(m1)")
    End Sub

    ''' <summary>
    ''' add some ||'s 
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Eval5()
        Dim list As New List(Of Macro)
        list.Add(New Macro("m1", "true"))
        list.Add(New Macro("m2", "false"))
        list.Add(New Macro("m3", "1"))
        list.Add(New Macro("m4", "0"))
        VerifyCondTrue(list, "foo || m1")
        VerifyCondFalse(list, "foo  || bar")
    End Sub

    ''' <summary>
    ''' Test some and's
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Eval6()
        Dim list As New List(Of Macro)
        list.Add(New Macro("m1", "true"))
        list.Add(New Macro("m2", "false"))
        list.Add(New Macro("m3", "1"))
        list.Add(New Macro("m4", "0"))
        VerifyCondTrue(list, "m1 && m2")
        VerifyCondFalse(list, "foo && m2")
        VerifyCondTrue(list, "m1 && 1")
    End Sub

    ''' <summary>
    ''' Complex ones
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Eval7()
        Dim list As New List(Of Macro)
        list.Add(New Macro("m1", "true"))
        list.Add(New Macro("m2", "false"))
        list.Add(New Macro("m3", "1"))
        list.Add(New Macro("m4", "0"))
        VerifyCondTrue(list, "defined(m1) || ab")
        VerifyCondFalse(list, "((m1) || m2) && foo")
    End Sub

    <Fact>
    Public Sub Eval8()
        Dim list As New List(Of Macro)
        list.Add(New Macro("m1", "true"))
        list.Add(New Macro("m2", "false"))
        list.Add(New Macro("m3", "1"))
        list.Add(New Macro("m4", "0"))
        VerifyCondFalse(list, "defined(m5)")
    End Sub

    <Fact>
    Public Sub Eval9()
        Dim list As New List(Of Macro)
        list.Add(New Macro("m1", "true"))
        list.Add(New Macro("m2", "false"))
        list.Add(New Macro("m3", "1"))
        list.Add(New Macro("m4", "0"))
        VerifyCondTrue(list, "!defined(m5)")
    End Sub

    ''' <summary>
    ''' Relational evaluation
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Eval10()
        Dim list As New List(Of Macro)
        list.Add(New Macro("m1", "5"))
        list.Add(New Macro("m2", "6"))
        VerifyCondTrue(list, "m2 > m1")
        VerifyCondTrue(list, "m2 >= m1")
    End Sub

    ''' <summary>
    ''' Relational operators with hex numbers
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Eval11()
        Dim list As New List(Of Macro)
        list.Add(New Macro("m1", "0x5"))
        list.Add(New Macro("m2", "0x6"))
        VerifyCondTrue(list, "m2 > m1")
        VerifyCondTrue(list, "m2 >= m1")
    End Sub

    ''' <summary>
    ''' Make sure we're replacing defined tokens
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Replace1()
        Dim before As String =
            "#define foo bar" & vbCrLf &
            "foo" & vbCrLf
        Dim after As String =
            "bar" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    <Fact>
    Public Sub Replace2()
        Dim before As String =
            "#define foo bar" & vbCrLf &
            "baz" & vbCrLf
        Dim after As String =
            "baz" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' C++ trick for introducing a comment.  Used in the definition for _VARIANT_BOOL
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Replace3()
        Dim before As String =
            "#define foo /##/ " & vbCrLf &
            "foo bar" & vbCrLf
        Dim after As String =
            "// bar" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    <Fact>
    Public Sub Replace4()
        Dim before As String =
            "#define m1(x) x##1 2" & vbCrLf &
            "m1(5)" & vbCrLf
        Dim after As String =
            "51 2" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' Simple macro method
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Method1()
        Dim before As String =
            "#define foo(x) x" & vbCrLf &
            "foo(1)"
        Dim after As String =
            "1" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' Couple of different replacements
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Method2()
        Dim before As String =
            "#define foo(x) x" & vbCrLf &
            "foo(1)" & vbCrLf &
            "foo(""hello"")" & vbCrLf &
            "foo(0x5)" & vbCrLf
        Dim after As String =
            "1" & vbCrLf &
            """hello""" & vbCrLf &
            "0x5" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' Whitespace junk
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Method3()
        Dim before As String =
            "#define foo(x)                   x" & vbCrLf &
            "foo(1)" & vbCrLf
        Dim after As String =
            "1" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' Several macros
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Method4()
        Dim before As String =
            "#define foo(x,y) x y" & vbCrLf &
            "foo(1,2)" & vbCrLf &
            "foo(""h"", ""y"")" & vbCrLf
        Dim after As String =
            "1 2" & vbCrLf &
            """hy""" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' Quote me test
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Method5()
        Dim before As String =
            "#define foo(x) #x" & vbCrLf &
            "foo(1)" & vbCrLf
        Dim after As String =
            """1""" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' Collapse side by side strings
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Method6()
        Dim before As String =
            "#define foo(x) #x" & vbCrLf &
            """h""foo(1)""y""" & vbCrLf
        Dim after As String =
            """h1y""" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' ## test
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Method7()
        Dim before As String =
            "#define foo(x) x##__" & vbCrLf &
            "foo(y)" & vbCrLf
        Dim after As String =
            "y__" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    <Fact>
    Public Sub Method8()
        Dim before As String =
            "#define foo(x,y) x##y" & vbCrLf &
            "foo(y,z)" & vbCrLf
        Dim after As String =
            "yz" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' Weird macro arguments
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Method9()
        Dim before As String =
                  "#define foo(x) x" & vbCrLf &
                  "foo(y(0))" & vbCrLf
        Dim after As String =
            "y(0)" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    <Fact>
    Public Sub Method10()
        Dim before As String =
            "#define foo(x,y) x ## y" & vbCrLf &
            "#define foo2(x,y) x## y" & vbCrLf &
            "#define foo3(x,y) x ##y" & vbCrLf &
            "foo(1,2)" & vbCrLf &
            "foo2(3,4)" & vbCrLf &
            "foo3(5,6)" & vbCrLf
        Dim after As String =
            "12" & vbCrLf &
            "34" & vbCrLf &
            "56" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' Recursive method calls
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Method11()
        Dim before As String =
            "#define inner(x) x" & vbCrLf &
            "#define outer(x) inner(x)" & vbCrLf &
            "outer(5)" & vbCrLf
        Dim after As String =
            "5" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' A more complex recursive method call
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Method12()
        Dim before As String =
            "#define inner(x,y) x##y" & vbCrLf &
            "#define outer(x,y) inner(x,y)" & vbCrLf &
            "outer(1,2)" & vbCrLf
        Dim after As String =
            "12" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' Make sure that quoted strings used as method macro arguments are properly
    ''' replaced in the string and collapsed
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Method13()
        Dim before As String =
            "#define x(y) ""foo"" y ""bar""" & vbCrLf &
            "x(""hey"")" & vbCrLf
        Dim after As String =
            """fooheybar""" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' Spacing between arguments needs to be maintained
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Method14()
        Dim before As String =
            "#define x(y) y" & vbCrLf &
            "x(a b)" & vbCrLf
        Dim after As String =
            "a b" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' When several items are passed as a single macro parameter make sure they still
    ''' go through replacement
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Method15()
        Dim before As String =
            "#define foo bar" & vbCrLf &
            "#define m1(x) x" & vbCrLf &
            "m1(foo 2)" & vbCrLf
        Dim after As String =
            "bar 2" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' Side by strings should collapse
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Misc1()
        Dim before As String =
            """foo""""bar""" & vbCrLf
        Dim after As String =
            """foobar""" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    ''' <summary>
    ''' Side by side strings with spaces should collapse
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub Misc2()
        Dim before As String =
            """foo""     ""bar""" & vbCrLf
        Dim after As String =
            """foobar""" & vbCrLf
        VerifyNormal(before, after)
    End Sub

    <Fact>
    Public Sub UnbalancedConditional1()
        Dim text As String =
            "#ifndef WINAPI"
        VerifyCount(text, 0, 0)
    End Sub

    <Fact>
    Public Sub UnbalancedConditional2()
        Dim text As String =
            "#ifdef WINAPI"
        VerifyCount(text, 1, 0)
    End Sub

    ''' <summary>
    ''' Make sure that permanent macros are preserved
    ''' </summary>
    ''' <remarks></remarks>
    <Fact>
    Public Sub PermanentMacro1()
        Dim list As New List(Of Macro)
        list.Add(New Macro("FOO", "BAR", True))
        VerifyNormal(
            list,
            "#define FOO BAZ" & vbCrLf &
            "FOO" & vbCrLf,
            "BAR" & vbCrLf)
    End Sub

End Class
