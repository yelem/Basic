using UnityEngine;

public class NewEmptyCSharpScript
{
    public void ExampleMethod()
    {
        // Using a feature from C# 9.0: Target-typed new expressions
        Vector3 position = new(0f, 0f, 0f);
        Debug.Log(position);
    }
}
