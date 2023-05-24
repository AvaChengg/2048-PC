using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Block : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private TextMeshPro _text;

    // private field
    [HideInInspector] public int Number;
    [HideInInspector] public Node Node;
    [HideInInspector] public Block MergingBlock;
    [HideInInspector] public bool IsMerged;

    public Vector2 Position => transform.position;

    public void Init(BlockType type)
    {
        Number = type.Number;
        _renderer.color = type.Color;
        _text.text = type.Number.ToString();
    }

    public void SetBlock(Node node)
    {
        if (Node != null) Node.OccupieBlock = null;
        Node = node;
        Node.OccupieBlock = this;
    }

    public void MergeBlock(Block blockToMergeWith)
    {
        // set the block we are merging with
        MergingBlock = blockToMergeWith;

        // set current node as unoccupied to allow blocks to use it
        Node.OccupieBlock = null;

        // set the base block as merging, so it does not get used twice
        blockToMergeWith.IsMerged = true;
    }

    public bool CanMerge(int number) => number == Number && !IsMerged && MergingBlock == null; 
}
