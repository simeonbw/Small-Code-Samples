using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundForestGenerator : MonoBehaviour
{
    [SerializeField] private GameObject _billboardPrefab;

    [SerializeField] private List<Material> _treeMaterials;

    [SerializeField] private float _spaceBetweenRows = 1f;

    [SerializeField] private List<TreeRow> _rows;
    
    public void GenerateForest()
    {
        //Delete old trees
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            int children = transform.GetChild(i).childCount;

            for (int c = children - 1; c >= 0; c--)
            {
                DestroyImmediate(transform.GetChild(i).GetChild(c).gameObject);
            }
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        foreach (TreeRow row in _rows)
        {
            GameObject r = new GameObject();

            r.name = "Row " + _rows.IndexOf(row);

            r.transform.SetParent(transform);

            r.transform.localPosition = Vector3.zero;

            float rowLen = (row.amountTrees - 1) * row.spaceBetweenTrees;

            float startPos = (rowLen / 2) * -1;

            for (int i = 0; i < row.amountTrees; i++)
            {
                float pos = startPos + (i * (row.spaceBetweenTrees));

                GameObject tree = Instantiate(_billboardPrefab, r.transform) as GameObject;

                if(_treeMaterials.Count != 0)
                {
                    tree.GetComponent<MeshRenderer>().material = _treeMaterials[Random.Range(0, _treeMaterials.Count - 1)];
                }
                
                tree.transform.rotation = transform.rotation;

                Vector3 position = (pos * (transform.right * -1)) + ((_rows.IndexOf(row) * _spaceBetweenRows) + Random.Range(row.zOffset.x, row.zOffset.y)) * (transform.forward * -1);

                tree.transform.localPosition = position;

                float scale = Random.Range(row.scaleOffset.x, row.scaleOffset.y);

                tree.transform.localScale = new Vector3(scale, scale, scale);
            }
        }
    }
}

[System.Serializable]
public struct TreeRow
{
    public int amountTrees;
    public float spaceBetweenTrees;
    public float rowOffset;
    public Vector2 zOffset;
    public Vector2 scaleOffset;
}