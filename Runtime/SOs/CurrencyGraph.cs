using UnityEngine;

public class CurrencyGraph : ScriptableObject
{
    public AnimationCurve Graph;

    public float Evaluate(float x)
    {
        return Graph.Evaluate(x);
    }
}
