using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VT
{
    [CustomEditor(typeof(FeedbackReader))]
	public class FeedbackReaderEditor : EditorBase
	{
        protected override void OnPlayingInspectorGUI()
        {
			var reader = (FeedbackReader)target;
            
			DrawTexture(reader.DebugTexture, "Mipmap Level Debug Texture");
        }
    }
}