using UnityEditor;
using UnityEngine;
using Unity.VisualScripting;
using UnityGLTF.Interactivity.VisualScripting.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    internal class AnimationStopNode : IUnitExporter
    {
        public System.Type unitType
        {
            get => typeof(InvokeMember);
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Animation), nameof(Animation.Stop),
                new AnimationStopNode());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            InvokeMember unit = unitExporter.unit as InvokeMember;

            GameObject target = UnitsHelper.GetGameObjectFromValueInput(
                unit.target, unit.defaultValues, unitExporter.exportContext);

            if (target == null)
            {
                UnitExportLogging.AddErrorLog(unit, "Can't resolve target GameObject");
                return false;
            }
            
            var animation = target.GetComponent<Animation>();
            if (!animation)
            {
                UnitExportLogging.AddErrorLog(unit, "Target GameObject does not have an Animation component.");
                return false;
            }

            var clip = animation.clip;
            if (unit.inputParameters.Count > 0)
            {
                if (unit.inputParameters[0].key == "%animation")
                {
                    if (!unitExporter.IsInputLiteralOrDefaultValue(unit.inputParameters[0], out var animationName))
                    {
                        UnitExportLogging.AddErrorLog(unit, "Animation name is not a literal or default value, which is not supported.");
                        return false;
                    }
                    
                    if (animationName is string animationNameString)
                    {
                        clip = animation.GetClip(animationNameString);
                        if (clip == null)
                        {
                            UnitExportLogging.AddErrorLog(unit, "Animation not found in Animation component.");
                            return false;
                        }
                    }
                }
            }
            
            int animationId = unitExporter.exportContext.exporter.GetAnimationId(clip, target.transform);

            if (animationId == -1)
            {
                UnitExportLogging.AddErrorLog(unit, "Animation not found in export context.");
                return false;
            }
            
            GltfInteractivityUnitExporterNode node = unitExporter.CreateNode(new Animation_StopNode());
            node.ValueSocketConnectionData[Animation_StopNode.IdValueAnimation].Value = animationId;

            
            unitExporter.MapInputPortToSocketName(unit.enter, Animation_StopNode.IdFlowIn, node);
            // There should only be one output flow from the Animator.Play node
            unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Animation_StopNode.IdFlowOut, node);
            return true;
        }
        
    }
}