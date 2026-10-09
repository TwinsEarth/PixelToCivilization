
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    static class ShaderGraphShortcuts
    {
        static MaterialGraphEditWindow GetFocusedShaderGraphEditorWindow()
        {
            MaterialGraphEditWindow[] windows = Resources.FindObjectsOfTypeAll<MaterialGraphEditWindow>();
            foreach (var window in windows)
            {
                if (window.hasFocus)
                    return window;
            }
            return null;
        }

        static GraphEditorView GetGraphEditorView()
        {
            MaterialGraphEditWindow window = GetFocusedShaderGraphEditorWindow();
            if (window != null)
            {
                return window.graphEditorView;
            }
            return null;
        }

        static MaterialGraphView GetGraphView()
        {
            GraphEditorView graphEditorView = GetGraphEditorView();
            if (graphEditorView != null)
            {
                return graphEditorView.graphView;
            }
            return null;
        }

        static HashSet<(KeyCode key, ShortcutModifiers modifier)> builtInShortCuts = new HashSet<(KeyCode key, ShortcutModifiers modifier)> {
                (KeyCode.A, ShortcutModifiers.None), // Frame All
                (KeyCode.F, ShortcutModifiers.None), // Frame Selection
                (KeyCode.Space, ShortcutModifiers.None), // Summon Searcher (for node creation)
                (KeyCode.C, ShortcutModifiers.Action), // Copy
                (KeyCode.X, ShortcutModifiers.Action), // cut
                (KeyCode.V, ShortcutModifiers.Action), // Paste
                (KeyCode.Z, ShortcutModifiers.Action), // Undo
                (KeyCode.Y, ShortcutModifiers.Action), // Redo
                (KeyCode.D, ShortcutModifiers.Action), // Duplicate
            };

        static void CheckBindings(string shortcutName)
        {
            if (!ShortcutManager.instance.IsShortcutOverridden(shortcutName))
                return;

            var customShortCut = ShortcutManager.instance.GetShortcutBinding(shortcutName);

            foreach (var shortcut in customShortCut.keyCombinationSequence)
            {
                if (builtInShortCuts.Contains((shortcut.keyCode, shortcut.modifiers)))
                {
                    throw new Exception($"The binding for {shortcutName} ({shortcut}) conflicts with a built-in shortcut. Please go to Edit->Shortcuts... and change the binding or reset to default.");
                }
            }
        }

        static bool GetMousePositionIsInGraphView(out Vector2 pos)
        {
            pos = default;
            var graphView = GetGraphView();
            var windowRoot = GetFocusedShaderGraphEditorWindow().rootVisualElement;
            var windowMousePosition = windowRoot.ChangeCoordinatesTo(windowRoot.parent, graphView.cachedMousePosition);

            if (!graphView.worldBound.Contains(windowMousePosition))
                return false; // don't create nodes if they aren't on the graph view.

            pos = graphView.contentViewContainer.WorldToLocal(graphView.cachedMousePosition);
            return true;
        }

        static void CreateNode<T>() where T : AbstractMaterialNode
        {
            if (!GetMousePositionIsInGraphView(out var graphMousePosition))
                return;

            var positionRect = new Rect(graphMousePosition, Vector2.zero);

            var graphView = GetGraphView();
            var graph = graphView.graph;
            AbstractMaterialNode node = Activator.CreateInstance<T>();

            var drawState = node.drawState;
            drawState.position = positionRect;
            node.drawState = drawState;

            graph.owner.RegisterCompleteObjectUndo("Add " + node.name);
            graphView.graph.AddNode(node);
        }

        [Shortcut("ShaderGraph/ShaderGraph: Save", typeof(MaterialGraphEditWindow), KeyCode.S, ShortcutModifiers.Action)]
        static void Save(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            GetFocusedShaderGraphEditorWindow().SaveAsset();
        }

        [Shortcut("ShaderGraph/ShaderGraph: Save As...", typeof(MaterialGraphEditWindow), KeyCode.S, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        static void SaveAs(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            GetFocusedShaderGraphEditorWindow().SaveAs();
        }

        [Shortcut("ShaderGraph/Selection: Open Documentation", typeof(MaterialGraphEditWindow), KeyCode.F1)]
        static void OpenDocumentation(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            foreach (var selected in GetGraphView().selection)
                if (selected is IShaderNodeView nodeView && nodeView.node.documentationURL != null)
                {
                    System.Diagnostics.Process.Start(nodeView.node.documentationURL);
                    break;
                }
        }

        [Shortcut("ShaderGraph/Selection: Group Selection", typeof(MaterialGraphEditWindow), KeyCode.G, ShortcutModifiers.Action)]
        static void GroupSelection(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            var graphView = GetGraphView();
            foreach (var selected in graphView.selection)
            {
                // Check if selected block node.
                // Only need check here since select material node and block node at the same time is not allowed
                // So if there is a selected block node means only selected block nodes.
                if (((GraphElement)selected).parent is ContextView)
                    return;

                if ((selected is IShaderNodeView nodeView && nodeView.node is AbstractMaterialNode) || selected.GetType() == typeof(Drawing.StickyNote))
                {
                    graphView.GroupSelection();
                    break;
                }
            }
        }

        [Shortcut("ShaderGraph/Selection: Ungroup Selection", typeof(MaterialGraphEditWindow), KeyCode.U, ShortcutModifiers.Action)]
        static void UnGroupSelection(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            var graphView = GetGraphView();
            foreach (var selected in graphView.selection)
                if ((selected is IShaderNodeView nodeView && nodeView.node is AbstractMaterialNode)
                    || selected.GetType() == typeof(Drawing.StickyNote))
                {
                    graphView.RemoveFromGroupNode();
                    break;
                }
        }

        [Shortcut("ShaderGraph/Selection: Toggle Node Previews", typeof(MaterialGraphEditWindow), KeyCode.P, ShortcutModifiers.Shift)]
        static void ToggleNodePreviews(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            bool shouldHide = false;
            // Toggle all node previews if none are selected. Otherwise, update only the selected node previews.
            var selection = GetGraphView().selection;
            if (selection.Count == 0)
            {
                var graph = GetGraphView().graph;
                var nodes = graph.GetNodes<AbstractMaterialNode>();
                foreach (AbstractMaterialNode node in nodes)
                    if (node.previewExpanded && node.hasPreview)
                    {
                        shouldHide = true;
                        break;
                    }
                
                graph.owner.RegisterCompleteObjectUndo("Toggle Previews");
                foreach (AbstractMaterialNode node in nodes)
                    node.previewExpanded = !shouldHide;
            }
            else
            {
                foreach (var selected in selection)
                    if (selected is IShaderNodeView nodeView)
                    {
                        if (nodeView.node.previewExpanded && nodeView.node.hasPreview)
                        {
                            shouldHide = true;
                            break;
                        }
                    }
                GetGraphView().SetPreviewExpandedForSelectedNodes(!shouldHide);
            }
        }

        #region Add Nodes

        [Shortcut("ShaderGraph/Add Node: Add", typeof(MaterialGraphEditWindow), KeyCode.A, ShortcutModifiers.Shift)]
        static void CreateAdd(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<AddNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Divide", typeof(MaterialGraphEditWindow), KeyCode.D, ShortcutModifiers.None)]
        static void CreateDivide(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<DivideNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Power", typeof(MaterialGraphEditWindow), KeyCode.E, ShortcutModifiers.None)]
        static void CreatePower(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<PowerNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Branch", typeof(MaterialGraphEditWindow), KeyCode.I, ShortcutModifiers.None)]
        static void CreateBranch(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<BranchNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Lerp", typeof(MaterialGraphEditWindow), KeyCode.L, ShortcutModifiers.None)]
        static void CreateLerp(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<LerpNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Multiply", typeof(MaterialGraphEditWindow), KeyCode.M, ShortcutModifiers.None)]
        static void CreateMultiply(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<MultiplyNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Normalize", typeof(MaterialGraphEditWindow), KeyCode.N, ShortcutModifiers.Alt)]
        static void CreateNormalize(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<NormalizeNode>();
        }

        [Shortcut("ShaderGraph/Add Node: One Minus", typeof(MaterialGraphEditWindow), KeyCode.O, ShortcutModifiers.None)]
        static void CreateOneMinus(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<OneMinusNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Reflection", typeof(MaterialGraphEditWindow), KeyCode.R, ShortcutModifiers.None)]
        static void CreateReflection(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<ReflectionNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Sample Texture 2D", typeof(MaterialGraphEditWindow), KeyCode.T, ShortcutModifiers.None)]
        static void CreateSampleTexture2D(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<SampleTexture2DNode>();
        }

        [Shortcut("ShaderGraph/Add Node: UV", typeof(MaterialGraphEditWindow), KeyCode.U, ShortcutModifiers.None)]
        static void CreateUV(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<UVNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Integer", typeof(MaterialGraphEditWindow), KeyCode.Alpha0, ShortcutModifiers.None)]
        static void CreateInteger(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<IntegerNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Boolean", typeof(MaterialGraphEditWindow), KeyCode.Alpha9, ShortcutModifiers.None)]
        static void CreateBoolean(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<BooleanNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Float", typeof(MaterialGraphEditWindow), KeyCode.Alpha1, ShortcutModifiers.None)]
        static void CreateFloat(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<Vector1Node>();
        }

        [Shortcut("ShaderGraph/Add Node: Vector2", typeof(MaterialGraphEditWindow), KeyCode.Alpha2, ShortcutModifiers.None)]
        static void CreateVec2(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<Vector2Node>();
        }

        [Shortcut("ShaderGraph/Add Node: Vector3", typeof(MaterialGraphEditWindow), KeyCode.Alpha3, ShortcutModifiers.None)]
        static void CreateVec3(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<Vector3Node>();
        }

        [Shortcut("ShaderGraph/Add Node: Vector4", typeof(MaterialGraphEditWindow), KeyCode.Alpha4, ShortcutModifiers.None)]
        static void CreateVec4(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<Vector4Node>();
        }

        [Shortcut("ShaderGraph/Add Node: Split", typeof(MaterialGraphEditWindow), KeyCode.B, ShortcutModifiers.None)]
        static void CreateSplit(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<SplitNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Tiling and Offset", typeof(MaterialGraphEditWindow), KeyCode.T, ShortcutModifiers.Alt)]
        static void CreateTilingAndOffset(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<TilingAndOffsetNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Time", typeof(MaterialGraphEditWindow), KeyCode.T, ShortcutModifiers.Shift)]
        static void CreateTime(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<TimeNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Position", typeof(MaterialGraphEditWindow), KeyCode.P, ShortcutModifiers.None)]
        static void CreatePosition(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<PositionNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Subtract", typeof(MaterialGraphEditWindow), KeyCode.S, ShortcutModifiers.None)]
        static void CreateSubtract(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<SubtractNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Combine", typeof(MaterialGraphEditWindow), KeyCode.V, ShortcutModifiers.None)]
        static void CreateCombine(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<CombineNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Saturate", typeof(MaterialGraphEditWindow), KeyCode.S, ShortcutModifiers.Shift)]
        static void CreateSaturate(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<SaturateNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Remap", typeof(MaterialGraphEditWindow), KeyCode.R, ShortcutModifiers.Shift)]
        static void CreateRemap(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<RemapNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Normal Vector", typeof(MaterialGraphEditWindow), KeyCode.N, ShortcutModifiers.None)]
        static void CreateNormalVector(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<NormalVectorNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Color", typeof(MaterialGraphEditWindow), KeyCode.Alpha5, ShortcutModifiers.None)]
        static void CreateColor(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<ColorNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Blend", typeof(MaterialGraphEditWindow), KeyCode.B, ShortcutModifiers.Shift)]
        static void CreateBlend(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<BlendNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Step", typeof(MaterialGraphEditWindow), KeyCode.Z, ShortcutModifiers.Alt)]
        static void CreateStep(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<StepNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Clamp", typeof(MaterialGraphEditWindow), KeyCode.C, ShortcutModifiers.None)]
        static void CreateClamp(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<ClampNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Smoothstep", typeof(MaterialGraphEditWindow), KeyCode.S, ShortcutModifiers.Alt)]
        static void CreateSmoothstep(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<SmoothstepNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Fresnel", typeof(MaterialGraphEditWindow), KeyCode.F, ShortcutModifiers.Shift)]
        static void CreateFresnel(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<FresnelNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Custom Function", typeof(MaterialGraphEditWindow), KeyCode.F, ShortcutModifiers.Alt)]
        static void CreateCFN(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<CustomFunctionNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Dot Product", typeof(MaterialGraphEditWindow), KeyCode.Period, ShortcutModifiers.None)]
        static void CreateDotProduct(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<DotProductNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Absolute", typeof(MaterialGraphEditWindow), KeyCode.B, ShortcutModifiers.Alt)]
        static void CreateAbsolute(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<AbsoluteNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Negate", typeof(MaterialGraphEditWindow), KeyCode.Minus, ShortcutModifiers.None)]
        static void CreateNegate(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<NegateNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Fraction", typeof(MaterialGraphEditWindow), KeyCode.R, ShortcutModifiers.Alt)]
        static void CreateFraction(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<FractionNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Swizzle", typeof(MaterialGraphEditWindow), KeyCode.W, ShortcutModifiers.None)]
        static void CreateSwizzle(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<SwizzleNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Gradient", typeof(MaterialGraphEditWindow), KeyCode.G, ShortcutModifiers.None)]
        static void CreateGradient(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<GradientNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Cross Product", typeof(MaterialGraphEditWindow), KeyCode.H, ShortcutModifiers.None)]
        static void CreateCrossProduct(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<CrossProductNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Floor", typeof(MaterialGraphEditWindow), KeyCode.L, ShortcutModifiers.Alt)]
        static void CreateFloor(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<FloorNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Ceiling", typeof(MaterialGraphEditWindow), KeyCode.C, ShortcutModifiers.Shift)]
        static void CreateCeiling(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<CeilingNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Sticky Note", typeof(MaterialGraphEditWindow), KeyCode.C, ShortcutModifiers.Alt)]
        static void CreateStickyNote(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            if (!GetMousePositionIsInGraphView(out var graphMousePosition))
                return;

            var graphView = GetGraphView();
            graphView.AddStickyNote(graphView.contentViewContainer.LocalToWorld(graphMousePosition));
        }
        
        #endregion
    }
}