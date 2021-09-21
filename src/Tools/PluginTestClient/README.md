# Plugin Test Client

## Use
This app is to test the various Azure actions/data-actions specified by your Plugin's RBAC requirements to make sure the plugin you write actually runs properly in SOS in a limited
context.

A list of actions/data-actions can be found [here](https://docs.microsoft.com/en-us/azure/role-based-access-control/resource-provider-operations).

### Syntax:
```

    @@@@@@@@  @@@@@@@@  @@@@@@@@
    @@@  @@@  @@@  @@@  @@@  @@@
    @@@  @@@  @@@  @@@  @@@  @@@
    @@@       @@@  @@@  @@@         SecOps Steward
    @@@@@@@@  @@@  @@@  @@@@@@@@    Plugin Test Harness
         @@@  @@@  @@@       @@@
    @@@  @@@  @@@  @@@  @@@  @@@
    @@@@ @@@  @@@  @@@  @@@ @@@@
      @@@@@@  @@@  @@@  @@@@@@@
          @@  @@@  @@@  @@
              @@@  @@@
                 @@

USAGE:
    PluginTestClient [OPTIONS]

OPTIONS:
    -h, --help             Prints help information
    -p, --path             Folder Path
    -c, --configuration    JSON Configuration
    -t, --tenant           Tenant ID
    -s, --subscription     Susbcription ID
```

### Example
`.\PluginTestClient --path C:\My\Plugin\bin\Debug\net5.0 --configuration "{'a':1,'b':2}" --tenant my-tenant-id --subscription my-subscription-id`

---

## Methodology
This program logs in simultaneously as a low-privilege and a high-privilege user:

**The High-Privilege User...**
* Creates a custom role in Azure with the actions specified for the plugin
* Runs the GRANT function to assign that role to the low-privilege user
* When test is complete, it runs the REVOKE function to remove the role assignment
* Deletes the custom role

**The Low-Privilege User...**
* Attempts to execute the plugin with _only_ the rights granted by the GRANT operation above