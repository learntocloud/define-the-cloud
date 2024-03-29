---
name: Submit a Cloud Definition
about: A definition to be displayed on LTC frontpage.
title: 'Submit a Cloud Definition'
labels: documentation, good first issue
assignees: madebygps

---

Do NOT copy/paste a definition from somewhere else. Read about the word you want to define and come up with your own definition. Copy/Paste submissions will be closed and not added.

Fill out the JSON with your submission:

```json
    {
        "Word": "network security group",
        "Content": "A set of rules used to filter network traffic in a virtual network and or subnet.",
        "Author": {
            "Name": "GPS",
            "Link": "https://youtube.com/madebygps"
            },
        "LearnMoreUrl": "https://learn.microsoft.com/en-us/azure/virtual-network/network-security-groups-overview",
        "Tag": "networking",
        "Abbreviation": "nsg"
    }
```
Fill out the JSON below with the following.

### Word (REQUIRED)

The word you are defining. Check [this URL](https://definethecloud.guide) for all words we currently have.

#### Content (REQUIRED)

The definition. No more than 4 sentences.

### learn more URL (REQUIRED)

Website where people can visit to learn more about the word.

### tag (REQUIRED and select one)

Tech category the word fits in. Options:

- compute
- security
- service
- general
- analytics
- developer tool
- web
- networking
- database
- storage
- devops
- ai/ml
- identity
- iot
- monitoring
- cost management
- disaster recovery

### abbreviation (OPTIONAL)

If the word is commonly abbreviated, please provide it. For example, command line interface is often abbreviated as CLI.

### author name (REQUIRED)

Your name.

### author link (REQUIRED)

The URL you want your name to link to.
