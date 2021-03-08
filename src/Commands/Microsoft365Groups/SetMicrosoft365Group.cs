﻿using PnP.Framework.Graph;
using PnP.PowerShell.Commands.Attributes;
using PnP.PowerShell.Commands.Base;
using PnP.PowerShell.Commands.Base.PipeBinds;
using PnP.PowerShell.Commands.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace PnP.PowerShell.Commands.Microsoft365Groups
{
    [Cmdlet(VerbsCommon.Set, "PnPMicrosoft365Group")]
    [RequiredMinimalApiPermissions("Group.ReadWrite.All")]
    public class SetMicrosoft365Group : PnPGraphCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public Microsoft365GroupPipeBind Identity;

        [Parameter(Mandatory = false)]
        public string DisplayName;

        [Parameter(Mandatory = false)]
        public string Description;

        [Parameter(Mandatory = false)]
        public String[] Owners;

        [Parameter(Mandatory = false)]
        public String[] Members;

        [Parameter(Mandatory = false)]
        public SwitchParameter IsPrivate;

        [Parameter(Mandatory = false)]
        [ValidateNotNullOrEmpty]
        [Alias("GroupLogoPath")]
        public string LogoPath;

        [Parameter(Mandatory = false)]
        public SwitchParameter CreateTeam;

        [Parameter(Mandatory = false)]
        public bool? HideFromAddressLists;

        [Parameter(Mandatory = false)]
        public bool? HideFromOutlookClients;

        protected override void ExecuteCmdlet()
        {
            var group = Identity.GetGroup(HttpClient, AccessToken, false, false);


            if (group != null)
            {
                bool changed = false;
                if (ParameterSpecified(nameof(DisplayName)))
                {
                    group.DisplayName = DisplayName;
                    changed = true;
                }
                if (ParameterSpecified(nameof(Description)))
                {
                    group.Description = Description;
                    changed = true;
                }
                if (ParameterSpecified(nameof(IsPrivate)))
                {
                    group.Visibility = IsPrivate ? "Private" : "Public";
                    changed = true;
                }
                if (changed)
                {
                    group = Microsoft365GroupsUtility.UpdateAsync(HttpClient, AccessToken, group).GetAwaiter().GetResult();
                }

                if (ParameterSpecified(nameof(Owners)))
                {
                    Microsoft365GroupsUtility.UpdateOwnersAsync(HttpClient, group.Id.Value, AccessToken, Owners).GetAwaiter().GetResult();
                }

                if (ParameterSpecified(nameof(Members)))
                {
                    Microsoft365GroupsUtility.UpdateMembersAsync(HttpClient, group.Id.Value, AccessToken, Members).GetAwaiter().GetResult();
                }

                if (ParameterSpecified(nameof(LogoPath)))
                {
                    if (!Path.IsPathRooted(LogoPath))
                    {
                        LogoPath = Path.Combine(SessionState.Path.CurrentFileSystemLocation.Path, LogoPath);
                    }
                    Microsoft365GroupsUtility.UploadLogoAsync(HttpClient, AccessToken, group.Id.Value, LogoPath).GetAwaiter().GetResult();
                }

                if (ParameterSpecified(nameof(CreateTeam)))
                {
                    if (!group.ResourceProvisioningOptions.Contains("Team"))
                    {
                        Microsoft365GroupsUtility.CreateTeamAsync(HttpClient, AccessToken, group.Id.Value).GetAwaiter().GetResult();
                    }
                    else
                    {
                        WriteWarning("There is already a provisioned Team for this group. Skipping Team creation.");
                    }
                }

                if (ParameterSpecified(nameof(HideFromAddressLists)) || ParameterSpecified(nameof(HideFromOutlookClients)))
                {
                    // For this scenario a separate call needs to be made
                    Microsoft365GroupsUtility.SetVisibilityAsync(HttpClient, AccessToken, group.Id.Value, HideFromAddressLists, HideFromOutlookClients).GetAwaiter().GetResult();
                }
            }
        }
    }
}