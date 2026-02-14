# Feature 072: Azure RM Parent-Child Resource Grouping

## Overview

Implements parent-child resource grouping for Azure RM resources, building on the framework established in Feature 068. Consolidates related Azure RM resources (VNet/subnets, DNS zones/records, route tables/routes, NSG/rules) into unified tables with inline diffs and character-level change highlighting.

## Scope

This feature focuses specifically on implementing the parent-child grouping framework for Azure Resource Manager (Azure RM) resources.

## Supported Resource Types

1. **Virtual Networks (`azurerm_virtual_network`) → Subnets (`azurerm_subnet`)**
   - Inline children: subnet attributes
   - Separate children: external subnet resources

2. **Route Tables (`azurerm_route_table`) → Routes (`azurerm_route`)**
   - Inline children: route attributes
   - Separate children: external route resources

3. **Network Security Groups (`azurerm_network_security_group`) → Security Rules (`azurerm_network_security_rule`)**
   - Inline children: security_rule attributes
   - Separate children: external security rule resources
   - 11-column table with split source/destination

4. **DNS Zones (`azurerm_dns_zone`, `azurerm_private_dns_zone`) → DNS Records**
   - Inline children: various DNS record type attributes
   - Separate children: external DNS record resources

## Key Features

- Character-level diff highlighting in HTML format
- Mixed management detection with warning indicators
- Conditional "Terraform Resource" column display
- Consistent value formatting with backticks
- Bare dash for null/empty values
- `<br>` tags for line breaks in simple diffs

## Related

- Feature 068: Parent-Child Resource Grouping Framework
- Feature 016: Network Security Group Parent-Child Grouping (restored capabilities)
