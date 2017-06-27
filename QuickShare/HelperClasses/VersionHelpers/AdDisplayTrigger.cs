using AdaptiveTriggerLibrary.ConditionModifiers.GenericModifiers;
using AdaptiveTriggerLibrary.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.HelperClasses.VersionHelpers
{
    public class AdDisplayTrigger : AdaptiveTriggerBase<bool, IGenericModifier>, IDynamicTrigger
    {
        public AdDisplayTrigger()
            : base (new EqualsModifier<bool>())
        {
            TrialSettings.IsTrialChanged += TrialSettings_IsTrialChanged;

            CurrentValue = TrialSettings.IsTrial;
        }

        private void TrialSettings_IsTrialChanged()
        {
            CurrentValue = TrialSettings.IsTrial;
        }

        public void ForceValidation()
        {
            CurrentValue = TrialSettings.IsTrial;
        }

        public void ResumeUpdates()
        {
            TrialSettings.IsTrialChanged += TrialSettings_IsTrialChanged;
        }

        public void SuspendUpdates()
        {
            TrialSettings.IsTrialChanged -= TrialSettings_IsTrialChanged;
        }
    }
}
