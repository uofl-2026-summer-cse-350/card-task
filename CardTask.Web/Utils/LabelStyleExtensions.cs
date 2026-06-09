namespace CardTask.Web.Utils;

public static class LabelStyleExtensions
{
    public static string GetLabelStyles(string label)
    {
        if (string.IsNullOrEmpty(label)) return "background-color: #e9ecef; color: #495057;";

        // Standard explicit overrides
        if (label == "Exam") return "background-color: #f8d7da; color: #842029;";
        if (label == "Homework") return "background-color: #fff3cd; color: #664d03;";
        if (label == "Assignment") return "background-color: #cfe2ff; color: #084298;";

        // Dynamic pastel color generator for custom labels
        int hash = 0;
        foreach (char c in label)
        {
            hash = c + (hash << 5) - hash;
        }

        int h = Math.Abs(hash) % 360;
        return $"background-color: hsl({h}, 75%, 92%); color: hsl({h}, 85%, 25%);";
    }
}