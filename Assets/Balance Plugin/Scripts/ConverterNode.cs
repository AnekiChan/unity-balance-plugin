using UnityEngine;

namespace BalancePlugin
{
    public class ConverterNode : BalancingNode
    {
        /*Converters (V) are advanced Nodes that convert one or more Resources into a specified output amount of Resources.*/
        public override string NodeType => "Converter";
        public override Color NodeColor => Color.yellow;

        public override void ProcessResources(BalancingData data)
        {
        }
    }
}