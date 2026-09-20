using UnityEngine;

/// <note>
/// The <see cref="AxisPropertyDrawer"/> class is used to draw the `Axis` fields in the Inspector using a custom property drawer.
/// Any changes to the serialized fields will not be automatically reflected in the Inspector unless the 
/// <see cref="AxisPropertyDrawer"/> is updated accordingly. If you add or modify any variables in `Axis`,
/// ensure the <see cref="AxisPropertyDrawer"/> logic is also updated to properly display and handle them in the Inspector.
/// </note>
[System.Serializable]
public struct Axis
{
    public float x;
    public float y;
    public float z;

    public Axis(float x, float y, float z)
    {
        this.x = Mathf.Clamp(x, -1, 1);
        this.y = Mathf.Clamp(y, -1, 1);
        this.z = Mathf.Clamp(z, -1, 1);
    }
    public Vector3 SetVector(Vector3 vector) => new Vector3(x * vector.x, y * vector.y, z * vector.y);


    public Vector3 Vector => new Vector3(x, y, z);
}
