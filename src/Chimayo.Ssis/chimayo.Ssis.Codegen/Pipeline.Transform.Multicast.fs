﻿module Chimayo.Ssis.CodeGen.Pipeline.Transform.Multicast

open Chimayo.Ssis.Common
open Chimayo.Ssis.Common.CustomOperators
open Chimayo.Ssis.CodeGen.Internals
open Chimayo.Ssis.CodeGen.CodeDsl
open Chimayo.Ssis.CodeGen.CodeDsl.Builder
open Chimayo.Ssis.CodeGen.Core
open Chimayo.Ssis.CodeGen.QuotationHelper
open Chimayo.Ssis.CodeGen.PipelineCore

open Chimayo.Ssis.Ast.ControlFlow
open Chimayo.Ssis.Ast.DataFlow
open Chimayo.Ssis.Ast.DataFlowApi


let buildInputConnection (DfInputConnection (DfName _, DfOutputReference (DfComponentReference cname, DfName oname))) =
    functionApplicationQ <@ Multicast.connect_input_by_name @> [ cname |> constant ; oname |> constant ] |> InlineExpression

let buildComponent (c : DfComponent) =
    let dummy = Chimayo.Ssis.Ast.DataFlowApi.Multicast.create "dummy" []
    let mutate eqOp getterFunction setterFunction mapper =
        eqOp (dummy |> getterFunction) (c |> getterFunction)
        |> [] @?@ [ functionApplication (getFullyQualifiedFunctionNameFromLambda setterFunction) [c |> getterFunction |> mapper] ]

    functionApplicationQ <@ Chimayo.Ssis.Ast.DataFlowApi.Multicast.create @>
        [
            c.name |> constant
            ListExpression
                [
                    yield! mutate stringCompareInvariant PipelineCommon.get_description <@ PipelineCommon.set_description @> constant
                    yield! mutate (=) PipelineCommon.get_locale_id <@ PipelineCommon.set_locale_id @> (makeOption true constant)
                    yield! mutate (=) PipelineCommon.get_validate_external_metadata <@ PipelineCommon.set_validate_external_metadata @> (makeOption true constant)
                    yield! mutate (=) PipelineCommon.get_uses_dispositions <@ PipelineCommon.set_uses_dispositions @> (makeOption true constant)

                    yield! c.outputs 
                           |> List.length
                           |> (=) 0 |> [] @?@ [
                                                    functionApplicationQ <@ Multicast.add_outputs @> 
                                                        [ c.outputs |> listExpression (fun (DfName name) -> constant name) ]
                                                    |> InlineExpression
                                              ]

                    yield! c.inputConnections |> List.map buildInputConnection

                ]
        ]


