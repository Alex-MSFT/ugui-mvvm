﻿using System;
#if UNITY_WSA || !NET_LEGACY
using ICommand = System.Windows.Input.ICommand;
#endif
using uguimvvm;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

[CustomEditor(typeof (CommandBinding))]
class CommandBindingEditor : Editor
{
    private SerializedProperty _parmprop;
    private SerializedProperty _vprop;
    private SerializedProperty _vmprop;
    private SerializedProperty _veprop;
    private string _focusedControl;

#region scene post processing
    [PostProcessScene(1)]
    public static void OnPostProcessScene()
    {
        FigureViewBindings();
    }

    private static void FigureViewBindings()
    {
        var objects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in objects)
        {
            var bindings = obj.GetComponents<CommandBinding>();
            foreach (var binding in bindings)
                FigureViewBinding(binding);
        }
    }

    static void FigureViewBinding(CommandBinding binding)
    {
        var sobj = new SerializedObject(binding);
        var vcprop = sobj.FindProperty("_view");
        var veprop = sobj.FindProperty("_viewEvent");
        if (string.IsNullOrEmpty(veprop.stringValue))
            return;

        var vcomp = vcprop.objectReferenceValue as Component;
        if (vcomp == null)
            return;

        var @event = INPCBindingEditor.GetEvent(vcomp, veprop);
        if (@event != null)
        {
            // Fixing adding multiple command binding event handlers when using prefabs
            var eventCount = @event.GetPersistentEventCount();
            for(var idx = 0; idx < eventCount; idx++)
            {
                var perTarget = @event.GetPersistentTarget(idx);
                // if we find a duplicate event skip over adding it
                if (perTarget == binding)
                {
                    return;
                }
            }
            
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(@event, binding.ExecuteCommand);
        }

        sobj.ApplyModifiedProperties();
    }
#endregion

    void OnEnable()
    {
        _vprop = serializedObject.FindProperty("_view");
        _vmprop = serializedObject.FindProperty("_viewModel");
        _veprop = serializedObject.FindProperty("_viewEvent");
        _parmprop = serializedObject.FindProperty("_parameter");
    }

    public override void OnInspectorGUI()
    {
        // Only update the focused element name during the Layout event, since all controls must be static between Layout & Repaint.
        if (Event.current.type == EventType.Layout)
        {
            _focusedControl = GUI.GetNameOfFocusedControl();
        }

        serializedObject.Update();

        EditorGUILayout.PropertyField(_vprop);
        if (_vprop.objectReferenceValue != null)
            INPCBindingEditor.DrawComponentEvents(_vprop, _veprop);

        INPCBindingEditor.DrawCRefProp(serializedObject.targetObject.GetInstanceID(), _focusedControl, _vmprop, GUIContent.none, typeof(ICommand));

        var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
        var label = EditorGUI.BeginProperty(rect, null, _parmprop);
        GUI.Label(rect, label);

        //parameter type
        var typeProp = _parmprop.FindPropertyRelative("Type");
        var trect = rect;
        trect.x += EditorGUIUtility.labelWidth;
        trect.width -= EditorGUIUtility.labelWidth;
        EditorGUI.PropertyField(trect, typeProp);

        //value field
        var typeValue = (BindingParameterType)Enum.GetValues(typeof(BindingParameterType)).GetValue(typeProp.enumValueIndex);
        SerializedProperty valueProp = null;
        switch(typeValue)
        {
            case BindingParameterType.None:
                break;
            default:
                valueProp = _parmprop.FindPropertyRelative(typeValue.ToString());
                break;
        }
        if (valueProp != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(valueProp);
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();

        serializedObject.ApplyModifiedProperties();
    }
}
