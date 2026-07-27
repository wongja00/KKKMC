using UnityEngine;
using UnityEngine.UIElements;

public class GridSystem : MonoBehaviour
{
    public GameObject straight;
    public GameObject crossroad;
    public GameObject deadEnd;
    public GameObject corner;

    public int width = 100;
    public int depth = 100;
    

    void Start()
    {
        for(int z = 0; z < depth; z += 10)
        {
            for(int x = 0; x < width; x += 10)
            {
                Vector3 position = new Vector3(x, 0, z);

                GameObject r;
                r = Instantiate(crossroad, position, Quaternion.identity);

                position.x = x;
                position.z = z + 10;

                r = Instantiate(straight, position, Quaternion.identity);

                position.x = x + 10;
                position.z = z;
                Quaternion q = Quaternion.Euler(0, 90, 0);
                r = Instantiate(straight, position, q);
            }
        }
    }

}
