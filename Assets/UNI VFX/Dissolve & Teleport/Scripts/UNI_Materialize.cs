using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UNI_Materialize : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMesh;
    public float materializeDuration = 4.0f;

    public void Start_in()
    {
        StartCoroutine(MaterializeIn());
    }

    public void Start_out()
    {
        StartCoroutine(MaterializeOut());
    }

    IEnumerator MaterializeIn()
    {
        float m_step = 1 / (materializeDuration * 50);
        float m_in = 0;
        while (m_in < 1)
        {
            m_in += m_step;
            skinnedMesh.material.SetFloat("_Materialize", m_in);
            yield return new WaitForSeconds(0.02f);
        }
    }
    IEnumerator MaterializeOut()
    {
        float m_step = 1 / (materializeDuration * 50);
        float m_out = 1;
        while (m_out > 0)
        {
            m_out -= m_step;
            skinnedMesh.material.SetFloat("_Materialize", m_out);
            yield return new WaitForSeconds(0.02f);
        }
    }
}
