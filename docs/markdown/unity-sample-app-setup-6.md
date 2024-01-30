# Additional Admin Consent Information
Depending on a tenant's configuration, before a user can sign-in, an admin may need first consent the application access to Teams, Microsoft Graph, and the custom web app API. By default admin consent isn't required for any of the resources used by the sample, however a tenant may have stricter policies in place that require admin consent.

It's possible to grant admin consent from within the application, using the built-in OAuth interface. However it may be easier to grant consent from outside the application. Outside consent is accomplished with the Azure [admin consent endpoint](https://docs.microsoft.com/azure/active-directory/develop/v2-admin-consent).

> For information on the Azure consent experience and framework read [Understanding Azure AD application consent experiences](https://docs.microsoft.com/azure/active-directory/develop/application-consent-experience) and [Microsoft identity platform consent framework](https://docs.microsoft.com/azure/active-directory/develop/consent-framework).

> For more information on granting tenant wide permissions read [Grant tenant-wide admin consent to an application](https://docs.microsoft.com/azure/active-directory/manage-apps/grant-admin-consent).

To complete this type of admin consent, the following pieces of information are needed:
  
* **Teams Directory (tenant) ID**. GUID is found on the [Azure Portal](https://portal.azure.com) for the Work/School Teams tenant. Go to the [Azure Activate Directory](https://portal.azure.com/#view/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/~/Overview) blade blade on the [Azure Portal](https://portal.azure.com), sign in with the Teams user, and copy the Teams user's tenant ID. If supporting multiple tenants, this ID is not necessarily the same tenant ID as the native/web application tenant ID.
  
* **Native application (client) ID**. GUID is found on the native [application registration](https://portal.azure.com/#view/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/~/RegisteredApps) overview section.
  
* **Web application (client) ID**. GUID is found on the web [application registration](https://portal.azure.com/#view/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/~/RegisteredApps) overview section. This is not needed if the native and web app share the same registration.
  
* **Web application Directory (tenant) ID**. Go to the [Azure Activate Directory](https://portal.azure.com/#view/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/~/Overview) blade blade on the [Azure Portal](https://portal.azure.com), sign in with the Azure app owner, and copy the tenant ID. If supporting multiple tenants, this ID is not necessarily the same tenant ID as the Teams tenant ID.
  
To grant admin consent outside of the native application:

1. If the web app and native app use unique app registrations, enter the following URL into a web browser. 
   
   <table style="max-width: 700px">
    <tr>
      <td><h4>Grant Permissions</h4></td>
      <td><h4>URL</h4></td>
    </tr>
    <tr>
      <td>All Web App's Default Permissions</td>
      <td style="word-break: break-all; word-wrap: break-word"><code>https://login.microsoftonline.com/{users-teams-tenant-id}/adminconsent?client_id={web-client-app-id}</code></td>
    </tr>
    <tr>
      <td>Reading MS Graph User Data</td>
      <td style="word-break: break-all; word-wrap: break-word"><code>https://login.microsoftonline.com/{users-teams-tenant-id}/v2.0/adminconsent?client_id={web-app-client-id}&scope=https://graph.microsoft.com/User.Read</code></td>
    </tr>
   </table>
      
2. If the web app consent was successful, the web browser will be redirected to a success page.

    ![Web page bar default admin consent succeeded for the web application.](./images/image-512-admin-consent-web-app-default.png)

3. Enter following URLs in a web browser. Only enter the URLs for the permissions you want to grant admin consent.
      
   <table style="max-width: 700px">
    <tr>
      <td><h4>Grant Permissions</45></td>
      <td><h4>URL</h4></td>
    </tr>
    <tr>
      <td>All Native App's Default Permissions</td>
      <td style="word-break: break-all; word-wrap: break-word"><code>https://login.microsoftonline.com/{users-teams-tenant-id}/adminconsent?client_id={native-app-client-id}</code></td>
    </tr>
    <tr>
      <td>Teams Meeting & Chat</td>
      <td style="word-break: break-all; word-wrap: break-word"><code>https://login.microsoftonline.com/{users-teams-tenant-id}/v2.0/adminconsent?client_id={native-app-client-id}&scope=https://auth.msft.communication.azure.com/Teams.ManageCalls+https://auth.msft.communication.azure.com/Teams.ManageChats</code></td>
    </tr>
    <tr>
      <td>Reading MS Graph Calendars, People, Chat, Online Meetings, and User Data</td>
      <td style="word-break: break-all; word-wrap: break-word"><code>https://login.microsoftonline.com/{users-teams-tenant-id}/v2.0/adminconsent?client_id={native-app-client-id}&scope=https://graph.microsoft.com/Calendars.Read+https://graph.microsoft.com/People.Read+https://graph.microsoft.com/User.Read+https://graph.microsoft.com/Chat.ReadWrite+https://graph.microsoft.com/OnlineMeetings.Read</code></td>
    </tr>
    <tr>
      <td>Custom Function App API<span style="color:red">*</span></td>
      <td style="word-break: break-all; word-wrap: break-word"><code>https://login.microsoftonline.com/{users-teams-tenant-id}/v2.0/adminconsent?client_id={native-app-client-id}&scope=api://{web-app-tenant-id}/{web-app-client-id}/user_impersonation</code> <span style="color:red">**</span></td>
    </tr>
   </table>

    <span style="color:red">*</span> Granting access to the Function App API is not required if the web and native app share the same app registration.

    <span style="color:red">**</span> The scope URI maybe different, depending on how the web app is exposing its API.
   
4. For each successful permission request, the web browser will be redirect to a URL with *admin_consent=true*.

    ![Web browser URL bar when default admin consent succeeded for the native application.](./images/image-511-admin-consent-native-app-default.png)


5. The sample application, if everything was successful, now has been granted admin consent. User's can now sign-in without be asked for admin consent.

## Publisher Verification
The walk-through assumes this sample application is being setup in a non-production tenant. This means that when granting admin consent, the sample app is marked as being from an unverified publisher. For production scenarios, the application publisher should be verified with Microsoft. For more information on this process visit the [Publisher Verification](https://docs.microsoft.com/azure/active-directory/develop/publisher-verification-overview) documents.

## Removing Admin Consent
If needed, the admin consent for the Teams tenant can be revoked. This can be accomplished from the [Enterprise Application](https://portal.azure.com/#view/Microsoft_AAD_IAM/StartboardApplicationsMenuBlade/~/AppAppsPreview/menuId~/null) tab on the [Azure Activate Directory](https://portal.azure.com/#view/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/~/Overview) blade, accessible from the [Azure Portal](https://portal.azure.com). For more information, visit [Delete an enterprise application](https://docs.microsoft.com/azure/active-directory/manage-apps/delete-application-portal?pivots=portal).

## Next Optional Step
The final optional step, [Configuring Sample with Authentication Key](./unity-sample-app-setup-5.md#configuring-sample-with-authentication-key), describes how to configure the app with an authentication key, instead of using AAD authentication.

