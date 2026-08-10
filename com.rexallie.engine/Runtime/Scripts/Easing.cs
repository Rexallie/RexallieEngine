using UnityEngine;

public static class Easing
{
    // A simple and effective quadratic ease-out function.
    // It starts fast and decelerates to a stop.
    public static float EaseOutQuad(float t)
    {
        // Clamps the input value between 0 and 1
        t = Mathf.Clamp01(t);
        return 1 - (1 - t) * (1 - t);
    }

    // You can add more easing functions here in the future!
    // For example, a smooth in-and-out curve:
    public static float EaseInOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t < 0.5f
            ? 4 * t * t * t
            : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
    }
}