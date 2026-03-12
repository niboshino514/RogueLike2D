using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DunGen.Editor.Drawers
{
	[CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
	public class SubclassSelectorDrawer : PropertyDrawer
	{
		private List<Type> derivedTypes;
		private bool initialized = false;

		private void Init(SerializedProperty property)
		{
			Type baseType = fieldInfo.FieldType;

			if (baseType.IsArray || (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(List<>)))
				baseType = baseType.GetGenericArguments()[0];

			derivedTypes = TypeCache.GetTypesDerivedFrom(baseType)
				.Where(t => !t.IsAbstract)
				.ToList();

			initialized = true;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			float lineHeight = EditorGUIUtility.singleLineHeight;

			if (!initialized)
				Init(property);

			// Build the list of options
			string[] options = new string[derivedTypes.Count + 1];
			options[0] = "None";
			for (int i = 0; i < derivedTypes.Count; i++)
			{
				var displayNameAttribute = derivedTypes[i].GetCustomAttribute<DisplayNameAttribute>();
				string displayName = displayNameAttribute != null ?
					displayNameAttribute.DisplayName :
					derivedTypes[i].Name;
				options[i + 1] = displayName;
			}

			// Figure out the current selection
			int selectedIndex = 0;
			bool hasChildProperties = false;
			if (!string.IsNullOrEmpty(property.managedReferenceFullTypename))
			{
				string[] split = property.managedReferenceFullTypename.Split(' ');
				if (split.Length == 2)
				{
					string currentTypeName = split[1];
					for (int i = 0; i < derivedTypes.Count; i++)
					{
						if (derivedTypes[i].FullName == currentTypeName)
						{
							selectedIndex = i + 1;

							// Check if the selected type has properties
							SerializedProperty copy = property.Copy();
							hasChildProperties = copy.NextVisible(true) && !SerializedProperty.EqualContents(copy, property.GetEndProperty());
							break;
						}
					}
				}
			}

			// Draw either foldout or label based on whether we have child properties
			Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, lineHeight);
			Rect popupRect = new Rect(labelRect.xMax, position.y, position.width - EditorGUIUtility.labelWidth, lineHeight);

			if (hasChildProperties)
			{
				property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);
			}
			else
			{
				EditorGUI.LabelField(labelRect, label);
			}

			int newIndex = EditorGUI.Popup(popupRect, selectedIndex, options);

			if (newIndex != selectedIndex)
			{
				if (newIndex == 0)
				{
					property.managedReferenceValue = null;
					property.serializedObject.ApplyModifiedProperties();
				}
				else
				{
					Type newType = derivedTypes[newIndex - 1];
					property.managedReferenceValue = Activator.CreateInstance(newType);
					property.serializedObject.ApplyModifiedProperties();
				}
			}

			// Draw child properties if expanded
			if (hasChildProperties && property.isExpanded)
			{
				EditorGUI.indentLevel++;

				Rect childRect = new Rect(position.x, position.y + lineHeight + EditorGUIUtility.standardVerticalSpacing,
					position.width, lineHeight);

				SerializedProperty copy = property.Copy();
				SerializedProperty endProperty = copy.GetEndProperty();

				copy.NextVisible(true);
				while (!SerializedProperty.EqualContents(copy, endProperty))
				{
					childRect.height = EditorGUI.GetPropertyHeight(copy, null, true);

					EditorGUI.BeginChangeCheck();
					EditorGUI.PropertyField(childRect, copy, true);
					if (EditorGUI.EndChangeCheck())
						property.serializedObject.ApplyModifiedProperties();

					childRect.y += childRect.height + EditorGUIUtility.standardVerticalSpacing;
					if (!copy.NextVisible(false))
						break;
				}

				EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = EditorGUIUtility.singleLineHeight; // For the type selector

			// Only add height for child properties if we have them and they're expanded
			if (!string.IsNullOrEmpty(property.managedReferenceFullTypename))
			{
				SerializedProperty copy = property.Copy();
				bool hasChildProperties = copy.NextVisible(true) && !SerializedProperty.EqualContents(copy, property.GetEndProperty());

				if (hasChildProperties && property.isExpanded)
				{
					SerializedProperty endProperty = property.GetEndProperty();

					do
					{
						height += EditorGUI.GetPropertyHeight(copy, null, true) + EditorGUIUtility.standardVerticalSpacing;
					} while (!SerializedProperty.EqualContents(copy, endProperty) && copy.NextVisible(false));
				}
			}

			return height;
		}
	}
}
