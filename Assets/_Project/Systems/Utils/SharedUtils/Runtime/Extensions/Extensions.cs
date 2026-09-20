using UnityEngine;

/// <summary>
/// Provides extension methods for the variables> type.
/// </summary>
public static class Extensions
{
    #region Number
    /// <summary>
    /// Checks if the value is between the minimum and maximum of a specified range.
    /// </summary>
    /// <param name="value">The original float value.</param>
    /// <param name="range">The <see cref="Vector2"/> representing the minimum and maximum values.</param>
    /// <returns>True if the value is between the range; otherwise, false.</returns>
    public static bool InRange(this float value, Vector2 range) => value > range.x && value < range.y;
    /// <summary>
    /// Checks if the value is between the minimum and maximum of a specified range.
    /// </summary>
    /// <param name="value">The original int value.</param>
    /// <param name="range">The <see cref="Vector2"/> representing the minimum and maximum values.</param>
    /// <returns>True if the value is between the range; otherwise, false.</returns>
    public static bool InRange(this int value, Vector2 range) => value >= range.x && value <= range.y;
    /// <summary>
    /// Checks if the value is between the minimum and maximum of a specified range.
    /// </summary>
    /// <param name="value">The original float value.</param>
    /// <param name="min">The <see cref="float"/> representing the minimum  value.</param>
    /// <param name="max">The <see cref="float"/> representing the maximum value.</param>
    /// <returns>True if the value is between the range; otherwise, false.</returns>
    public static bool InRange(this float value, float min, float max) => value > min && value < max;
    /// <summary>
    /// Checks if the value is between the minimum and maximum of a specified range.
    /// </summary>
    /// <param name="value">The original int value.</param>
    /// <param name="min">The <see cref="float"/> representing the minimum  value.</param>
    /// <param name="max">The <see cref="float"/> representing the maximum value.</param>
    /// <returns>True if the value is between the range; otherwise, false.</returns>
    public static bool InRange(this int value, int min, int max) => value >= min && value <= max;
    /// <summary>
    /// Set new Vector3 object with modified x, y, or z components based on provided values.
    /// </summary>
    /// <param name="vector">The original Vector3 instance.</param>
    /// <param name="x">The new value for the x-coordinate.</param>
    /// <param name="y">The new value for the y-coordinate.</param>
    /// <param name="z">The new value for the z-coordinate.</param>
    /// <returns>A new Vector3 object with updated x, y, or z values.</returns>
    #endregion
    #region Vectors
    public static Vector3 With(this Vector3 vector, float? x = null, float? y = null, float? z = null) => new Vector3(x ?? vector.x, y ?? vector.y, z ?? vector.z);

    /// <summary>
    /// Adds specified values to the x, y, and z components of the vector.
    /// </summary>
    /// <param name="vector">The Vector3 instance to be added to.</param>
    /// <param name="x">The value to add to the x-coordinate.</param>
    /// <param name="y">The value to add to the y-coordinate.</param>
    /// <param name="z">The value to add to the z-coordinate.</param>
    /// <returns>A new Vector3 object resulting from the addition of x, y, and z to the original vector.</returns>
    public static Vector3 Add(this Vector3 vector, float x = 0, float y = 0, float z = 0) => new Vector3(vector.x + x, vector.y + y, vector.z + z);
    /// <summary>
    /// Divides the components of the current Vector3 by the corresponding components of the provided Vector3.
    /// </summary>
    /// <param name="vector">The original Vector3 instance.</param>
    /// <param name="divider">The Vector3 instance by which the components of the original vector will be divided.</param>
    /// <returns>A new Vector3 object resulting from the division of each component of the original vector by the corresponding component of the divider.</returns>
    public static Vector3 Divide(this Vector3 vector, Vector3 divider) => new Vector3(vector.x / divider.x, vector.y / divider.y, vector.z / divider.z);
    /// <summary>
    /// Multiply the components of the current Vector3 by the corresponding components of the provided Vector3.
    /// </summary>
    /// <param name="vector">The original Vector3 instance.</param>
    /// <param name="multiplier">The Vector3 instance by which the components of the original vector will be multiplied.</param>
    /// <returns>A new Vector3 object resulting from the multiplication of each component of the original vector by the corresponding component of the multiplier.</returns>
    public static Vector3 Multiply(this Vector3 vector, Vector3 multiplier) => new Vector3(vector.x * multiplier.x, vector.y * multiplier.y, vector.z * multiplier.z);

    /// <summary>
    /// Determines whether a target is within the field of view (FOV) of a given direction.
    /// </summary>
    /// <param name="position">The starting position from which the field of view is calculated.</param>
    /// <param name="direction">The forward direction representing the center of the field of view.</param>
    /// <param name="target">The position of the target to check.</param>
    /// <param name="angle">The total angle of the field of view in degrees.</param>
    /// <returns>True if the target is within the field of view; otherwise, false.</returns>
    #endregion
    #region Quaternion
    public static float GetRotationAngleDirection(this Quaternion rotation, out bool forward, Vector3 axis, bool use360Angle = false)
    {
        axis.Normalize();
        Vector3 quaternionVec = new Vector3(rotation.x, rotation.y, rotation.z);
        Vector3 projection = Vector3.Dot(quaternionVec, axis) * axis;
        Quaternion twist = new Quaternion(projection.x, projection.y, projection.z, rotation.w);
        twist = Quaternion.Normalize(twist);
        twist.ToAngleAxis(out float angle, out Vector3 twistAxis);
        forward = Vector3.Dot(twistAxis, axis) >= 0f;
        if (use360Angle)
            //return Vector3.Dot(twistAxis, axis) >= 0f ? angle : 360f - angle;
            return angle;
        angle = Mathf.DeltaAngle(0f, angle);
        if (Vector3.Dot(twistAxis, axis) < 0f)
            angle = -angle;
        return angle;
    }
    #endregion
    #region Arrays

    #endregion
    #region Game
    public static bool IsInFOV(this Vector3 position, Vector3 direction, Vector3 target, float angle)
    {
        Vector3 directionToTarget = target - position;
        float currentAngle = Vector3.Angle(direction, directionToTarget);
        return currentAngle < angle / 2;
    }
    public static float Angle(this Vector3 position, Vector3 direction, Vector3 target)
    {
        Vector3 directionToTarget = target - position;
        return Vector3.Angle(direction, directionToTarget);
    }
    /// <summary>
    /// Calculates the position of a value within a range, normalized to a 0-1 scale.
    /// </summary>
    /// <param name="value">The value to position within the range.</param>
    /// <param name="range">The <see cref="Vector2"/> representing the range (min and max).</param>
    /// <returns>The normalized position of the value within the range.</returns>
    public static float GetPositionInRange(this float value, Vector2 range) => (value - range.x) / (range.y - range.x);
    /// <summary>
    /// Determines whether the difference between the current value and a specified value is less than a given minimum distance.
    /// </summary>
    /// <param name="value">The original float value.</param>
    /// <param name="valueToCompare">The float value to compare against.</param>
    /// <param name="minDistance">The minimum distance threshold to check.</param>
    /// <returns>
    /// <c>true</c> if the absolute difference between <paramref name="value"/> 
    /// and <paramref name="valueToCompare"/> is less than <paramref name="minDistance"/>; 
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsCloser(this float value,float valueToCompare, float minDistance)
    {
        float distance = value - valueToCompare;
        return Mathf.Abs(distance) < minDistance;
    }
    /// <summary>
    /// Sets the global scale of the transform to match the target transform's global scale.
    /// </summary>
    /// <param name="source">The source transform to update.</param>
    /// <param name="target">The target transform to match the global scale of.</param>
    public static void SetGlobalScale(this Transform source, Vector3 targetScale)
    {
        Vector3 parentScale = source.parent ? source.parent.lossyScale : Vector3.one;
        source.localScale = targetScale.Divide(parentScale);
    }
    #endregion
}
