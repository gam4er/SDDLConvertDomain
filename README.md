# README: SDDL Convert Domain Console Tool

---

## Overview

The **SDDL Convert Domain Console Tool** is designed to help administrators and security professionals parse and analyze **nTSecurityDescriptor** fields, identifying critical security permissions (Access Control Entries, or ACEs) assigned to Active Directory objects. It flags suspicious or overly permissive access rights that may have been granted to broad groups (such as **Domain Users**, **Authenticated Users**, or **Everyone**) in a domain environment.

The tool parses SDDL (Security Descriptor Definition Language) strings, extracts the Access Control Entries (ACEs), and checks for potentially dangerous or critical permissions that could lead to privilege escalation, unauthorized access, or security policy violations.

---

## Key Features

- **SDDL Parsing**: Converts SDDL strings to readable formats and extracts key security properties.
- **Lookup for Rights and SIDs**: Resolves GUIDs, SIDs, and permission codes into human-readable values.
- **Detection of Suspicious Rights**: Highlights overly broad permissions or dangerous access rights, such as **Generic All**, **Write Owner**, or **Write DAC**, assigned to wide-ranging groups.
- **Verbose Mode**: Provides detailed output about the parsed SDDL string, including individual ACEs and their explanations.

---

## Usage

### Arguments

The tool can be executed from the command line with several options:

- `-s` or `--SDDL`  
  Specify the SDDL string to be processed.
  ```bash
  -s "O:BAG:SYD:(A;;GA;;;S-1-1-0)"
  ```

- `--verbose`  
  Enables verbose output, providing detailed information on each ACE found, including type, rights, and associated SIDs.

- `--stdin`  
  Reads the SDDL string directly from the standard input if not provided through the `-s` option.

### Example Usage

```bash
SDDLConvertDomainConsole.exe -s "O:BAG:SYD:(A;;GA;;;S-1-1-0)" --verbose
```

### Output
The tool will print parsed information, such as:
- **Owner SID**
- **Group SID**
- **Discretionary ACL (DACL) and System ACL (SACL) flags**
- Details on each ACE, including the type of access rights and the associated group or user

The tool will highlight **suspicious ACEs**, such as those that grant **Generic Write** or **Full Control** to broad groups like **Domain Users** or **Everyone**.

---

## How It Works

1. **SDDL Parsing**: The tool reads the provided SDDL string and extracts ownership, group details, and all defined ACEs.
2. **GUID/SID Lookup**: For each ACE, the tool looks up the associated GUIDs and SIDs to determine which users or groups have been granted permissions.
3. **Right Decoding**: It decodes the access rights into human-readable formats, such as **Generic Write** or **Write Owner**.
4. **Suspicious Access Identification**: If dangerous or overly broad permissions are detected (e.g., **Generic All** for **Everyone**), they are flagged for review.

---

## How to Understand the Output

- **Legitimate Access**: Access granted to administrators or service accounts with specific, limited rights (e.g., read-only access) is typically considered normal.
- **Suspicious Access**: If broad permissions such as **Generic All** or **Write Owner** are granted to general groups like **Domain Users** or **Authenticated Users**, this may be a sign of a misconfiguration or security risk.

---

## Best Practices

- Always review the output for overly permissive ACEs, especially those affecting large groups like **Domain Users**.
- Use the **verbose mode** to get detailed insights into each ACE and ensure legitimate permissions.
- Combine this tool with regular security audits to keep your Active Directory permissions in check.

---
