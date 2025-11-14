using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class Label : MonoBehaviour
{
	public RectTransform rt;
    public TMP_Text labelShadow;
	public TMP_Text label;
	
	public void ChangeText(string newText, bool filterRichText = false)
	{
		if(filterRichText)
		{
			labelShadow.text = RemoveRichTextTags(newText);
		}
		else
		{
			labelShadow.text = newText;
		}
		label.text = newText;
	}
	
	public string RemoveRichTextTags(string input)
	{
		string pattern = @"<.*?>";
		return Regex.Replace(input, pattern, string.Empty);
	}
	
	public void ForceMeshUpdate()
	{
		labelShadow.ForceMeshUpdate(true, true);
		label.ForceMeshUpdate(true, true);
	}
}
