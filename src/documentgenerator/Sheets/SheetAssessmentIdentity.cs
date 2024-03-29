using Syncfusion.XlsIO;
using ZeroTrustAssessment.DocumentGenerator.Graph;

namespace ZeroTrustAssessment.DocumentGenerator.Sheets;

public class SheetAssessmentIdentity : SheetBase
{
    private const string RoleIdGlobalAdmin = "62e90394-69f5-4237-9190-012177145e10";
    public SheetAssessmentIdentity(IWorkbook workbook, ZtSheets sheet, GraphData graphData) : base(workbook, sheet, graphData)
    {
    }

    public AssessmentScore Generate()
    {        
        WorkloadChecks();
        TenantAppManagementPolicy();
        GlobalAdminPhishingResistantAuthStrength();
        BasicMfaUsageInsteadOfAuthStrength();
        SecureRegistrationOfSecurityInfo();
        CloudOnlyCloudPrivilege();
        RegistrationCampaign();
        PhishingResistantAuthMethods();
        PasswordProtection();
        PimJustInTime();
        ConditionalAccess();

        ShowScore();
        return GetAssessmentScore();
    }

    private void WorkloadChecks()
    {
        var hasWorkloadPolicies = _graphData.ConditionalAccessPolicies.Any(
            x => x.State == ConditionalAccessPolicyState.Enabled &&
            x?.Conditions?.ClientApplications != null && x?.Conditions?.ClientApplications?.IncludeServicePrincipals?.Count > 0);

        var result = hasWorkloadPolicies ? AssessmentValue.Completed : AssessmentValue.NotStartedP1;
        SetValue("I00001_Workload_Exists", result);
    }

    private void TenantAppManagementPolicy()
    {
        var appPolicy = _graphData.TenantAppManagementPolicy;
        var hasTenantPolicy = appPolicy != null && appPolicy.IsEnabled == true;

        var result = hasTenantPolicy ? AssessmentValue.Completed : AssessmentValue.NotStartedP1;
        SetValue("I00002_Workload_TenantAppMgmtPolicy", result);
    }

    private void GlobalAdminPhishingResistantAuthStrength()
    {
        //TODO Check if auth strength is actually a phishing reistant auth strength

        var globalAdminPoliciesWithMfa = _graphData.ConditionalAccessPolicies.Any(
            x => x.State == ConditionalAccessPolicyState.Enabled &&
                x?.Conditions?.Users?.IncludeRoles?.Contains(RoleIdGlobalAdmin) == true &&
                x?.GrantControls?.AuthenticationStrength != null);

        var result = globalAdminPoliciesWithMfa ? AssessmentValue.Completed : AssessmentValue.NotStartedP1;
        SetValue("I00003_GlobalAdminPhishingResistantAuthStrength", result);
    }

    private void BasicMfaUsageInsteadOfAuthStrength()
    {
        var basicMfaInUse = _graphData.ConditionalAccessPolicies.Any(
            x => x.State == ConditionalAccessPolicyState.Enabled &&
            x?.GrantControls?.BuiltInControls?.Contains(ConditionalAccessGrantControl.Mfa) == true);

        var resultNoAuthStrength = basicMfaInUse ? AssessmentValue.NotStartedP2 : AssessmentValue.Completed;

        var hasAuthStrength = _graphData.ConditionalAccessPolicies.Any(
            x => x.State == ConditionalAccessPolicyState.Enabled &&
            x?.GrantControls?.AuthenticationStrength != null);

        var resultBasicMfa = basicMfaInUse || hasAuthStrength ? AssessmentValue.Completed : AssessmentValue.Completed;

        SetValue("I00004_BasicMfaUsageInsteadOfAuthStrength", resultNoAuthStrength);
        SetValue("I00005_BasicMfaInUse", resultBasicMfa);
    }

    private void SecureRegistrationOfSecurityInfo()
    {
        var hasSecurityInfo = _graphData.ConditionalAccessPolicies.Any(
            x => x.State == ConditionalAccessPolicyState.Enabled &&
            x?.Conditions?.Applications?.IncludeUserActions?.Contains("urn:user:registersecurityinfo") == true);

        var hasSecurityInfoAndDevice = _graphData.ConditionalAccessPolicies.Any(
            x => x.State == ConditionalAccessPolicyState.Enabled &&
            x?.Conditions?.Applications?.IncludeUserActions?.Contains("urn:user:registersecurityinfo") == true &&
            (x?.GrantControls?.BuiltInControls?.Contains(ConditionalAccessGrantControl.CompliantDevice) == true ||
            x?.GrantControls?.BuiltInControls?.Contains(ConditionalAccessGrantControl.DomainJoinedDevice) == true)
            );

        var result = hasSecurityInfoAndDevice ? AssessmentValue.Completed :
                       hasSecurityInfo ? AssessmentValue.NotStartedP1 : AssessmentValue.NotStartedP0;

        SetValue("I00006_SecureRegistrationOfSecurityInfo", result);
    }

    private void CloudOnlyCloudPrivilege()
    {
        var globalAdminRoles = _graphData.RoleAssignments.Where(x => x?.RoleDefinitionId == RoleIdGlobalAdmin);
        var jitUsers = _graphData.RoleEligibilitySchedule.Where(x => x?.RoleDefinitionId == RoleIdGlobalAdmin);
        var nonJitUsers = _graphData.RoleAssignmentSchedule.Where(x => x?.RoleDefinitionId == RoleIdGlobalAdmin);

        var hasGlobalAdminSyncedAccounts = HasSyncedAccounts(globalAdminRoles);
        if (!hasGlobalAdminSyncedAccounts)
        {
            hasGlobalAdminSyncedAccounts = HasSyncedAccounts(nonJitUsers);
            if (!hasGlobalAdminSyncedAccounts)
            {
                hasGlobalAdminSyncedAccounts = HasSyncedAccounts(jitUsers);
            }
        }
        var hasPrivilegedRolesSyncedAccounts = false; //todo lookup other roles
        var result = hasGlobalAdminSyncedAccounts ? AssessmentValue.NotStartedP0 :
                       hasPrivilegedRolesSyncedAccounts ? AssessmentValue.NotStartedP1 : AssessmentValue.Completed;

        SetValue("I00007_CloudOnlyCloudPrivilege", result);
    }

    private void RegistrationCampaign()
    {
        var authPolicy = _graphData.AuthenticationMethodsPolicy;

        var result = authPolicy != null &&
            authPolicy.RegistrationEnforcement?.AuthenticationMethodsRegistrationCampaign?.State == AdvancedConfigState.Enabled
            ? AssessmentValue.Completed : AssessmentValue.NotStartedP1;

        SetValue("I00008_RegistrationCampaign", result);
    }

    private void PhishingResistantAuthMethods()
    {
        var authPolicy = _graphData.AuthenticationMethodsPolicy;

        bool hasFido2 = false;
        bool hasCba = false;
        bool hasSms = false;
        bool hasVoice = false;

        if (authPolicy?.AuthenticationMethodConfigurations != null)
        {
            foreach (var method in authPolicy.AuthenticationMethodConfigurations)
            {
                if (method is Fido2AuthenticationMethodConfiguration fido2)
                {
                    if (method.State == AuthenticationMethodState.Enabled)
                    {
                        hasFido2 = true;
                    }
                }
                else if (method is X509CertificateAuthenticationMethodConfiguration cba)
                {
                    if (method.State == AuthenticationMethodState.Enabled)
                    {
                        hasCba = true;
                    }
                }
                else if (method is VoiceAuthenticationMethodConfiguration voice)
                {
                    if (method.State == AuthenticationMethodState.Enabled)
                    {
                        hasVoice = true;
                    }
                }
                else if (method is SmsAuthenticationMethodConfiguration sms)
                {
                    if (method.State == AuthenticationMethodState.Enabled)
                    {
                        hasSms = true;
                    }
                }
            }
        }
        var resultPhishingResistant = AssessmentValue.Completed;
        if (!hasFido2 && !hasCba)
        {
            resultPhishingResistant = AssessmentValue.NotStartedP1;
        }
        else if (!hasFido2)
        {
            resultPhishingResistant = AssessmentValue.NotStartedP2;
        }
        SetValue("I00009_PhishingResistantAuthMethods", resultPhishingResistant);

        AssessmentValue resultWeakAuth = AssessmentValue.NotStartedP2;
        if (authPolicy?.PolicyMigrationState == AuthenticationMethodsPolicyMigrationState.MigrationComplete)
        {
            if (!hasSms && !hasVoice)
            {
                resultWeakAuth = AssessmentValue.Completed;
            }
        }
        SetValue("I00010_WeakAuthMethods", resultWeakAuth);
    }

    private void PasswordProtection()
    {
        var enableBannedPasswordCheckOnPremises = _graphData.GetDirectorySettingBool(
            DirectorySettingTemplateEnum.PasswordRuleSettings, "EnableBannedPasswordCheckOnPremises");
        var passwordProtectOnPrem = enableBannedPasswordCheckOnPremises == true ?
            AssessmentValue.Completed : AssessmentValue.NotStartedP1;
        SetValue("I00011_PasswordProtectOnPrem", passwordProtectOnPrem);


        var enableBannedPasswordCheck = _graphData.GetDirectorySettingBool(DirectorySettingTemplateEnum.PasswordRuleSettings, "EnableBannedPasswordCheck");
        var hasBannedPasswordList = AssessmentValue.NotStartedP1;
        if (enableBannedPasswordCheck == true)
        {
            var bannedPasswordList = _graphData.GetDirectorySetting(DirectorySettingTemplateEnum.PasswordRuleSettings, "BannedPasswordList");
            if (!string.IsNullOrEmpty(bannedPasswordList))
            {
                hasBannedPasswordList = AssessmentValue.Completed;
            }
        }
        SetValue("I00012_BannedPasswordList", hasBannedPasswordList);

    }

    private void PimJustInTime()
    {
        var eligible = _graphData.RoleEligibilitySchedule;
        var assigned = _graphData.RoleAssignmentSchedule;

        var pimEnabledCritical = AssessmentValue.NotStartedP1;

        //Get all users eligible with JIT
        var jitUsers = eligible.Where(x => x?.RoleDefinitionId == RoleIdGlobalAdmin);

        //Get all the users that are in this role without JIT activation.
        var nonJitUsers = assigned.Where(x => x?.RoleDefinitionId == RoleIdGlobalAdmin
            && x?.AssignmentType != "Activated");

        if (jitUsers?.Count() > 0 && nonJitUsers?.Count() == 0)
        {
            pimEnabledCritical = AssessmentValue.Completed;
        }

        SetValue("I00013_PIM_JIT_CriticalRoles", pimEnabledCritical);
    }

    private void ConditionalAccess()
    {
        var hasAppProtection = _graphData.ConditionalAccessPolicies.Any(
            x => x.State == ConditionalAccessPolicyState.Enabled &&
            x?.GrantControls?.BuiltInControls?.Any(
                y => y != null && y.Value == ConditionalAccessGrantControl.CompliantApplication) == true);

        var hasAppProtectionResult = hasAppProtection ? AssessmentValue.Completed : AssessmentValue.NotStartedP1;

        SetValue("I00014_CA_AppProtection", hasAppProtectionResult);


        var hasCompliantDevice = _graphData.ConditionalAccessPolicies.Any(
            x => x.State == ConditionalAccessPolicyState.Enabled &&
            x?.GrantControls?.BuiltInControls?.Any(
                y => y != null && (
                    y.Value == ConditionalAccessGrantControl.CompliantDevice ||
                    y.Value == ConditionalAccessGrantControl.DomainJoinedDevice
                )) == true);

        var hasCompliantDeviceResult = hasCompliantDevice ? AssessmentValue.Completed : AssessmentValue.NotStartedP1;

        SetValue("I00015_CA_CompliantDevice", hasCompliantDeviceResult);
    }

    private bool HasSyncedAccounts(IEnumerable<UnifiedRoleAssignment> roleAssignments)
    {
        foreach (var role in roleAssignments)
        {
            if (IsSyncedUser(role.Principal)) return true;
        }
        return false;
    }

    private bool HasSyncedAccounts(IEnumerable<UnifiedRoleAssignmentSchedule> roleAssignments)
    {
        foreach (var role in roleAssignments)
        {
            if (IsSyncedUser(role.Principal)) return true;
        }
        return false;
    }
    private bool HasSyncedAccounts(IEnumerable<UnifiedRoleEligibilitySchedule> roleAssignments)
    {
        foreach (var role in roleAssignments)
        {
            if (IsSyncedUser(role.Principal)) return true;
        }
        return false;
    }
    private bool IsSyncedUser(DirectoryObject? principal)
    {
        if (principal is User user)
        {
            if (user.OnPremisesSyncEnabled == true) return true;
        }
        else if (principal is Group group)
        {
            var graphGroup = _graphData.GetGroup(group.Id, true);
            if (graphGroup?.Members != null)
            {
                foreach (var member in graphGroup.Members)
                {
                    if (IsSyncedUser(member)) return true;
                }
            }
            var eligibleGroup = _graphData.GetPrivilegedAccessGroupEligibilitySchedule(group.Id);
            if (eligibleGroup != null)
            {
                foreach (var pag in eligibleGroup)
                {
                    if (IsSyncedUser(pag.Principal)) return true;
                }
            }
        }
        return false;
    }
}