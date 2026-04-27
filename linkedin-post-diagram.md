# LinkedIn Post Diagram - Human-in-the-Loop
# Copy the code below and paste it at https://mermaid.live to generate the image

```mermaid
flowchart TD
    A["📥 Contract Input"] --> B["✅ Validation & Normalization"]
    B --> C["🔍 Scrape Competitor Offers"]
    C --> D["🤖 AI Analysis\n(Semantic Kernel + OpenAI)"]
    D --> E["📊 Decision:\nKeep or Switch?"]
    
    E --> F["⏸️ WORKFLOW PAUSES"]
    
    F --> G{"👤 Human Reviews\nAI Recommendation"}
    
    G -->|"✅ Approve"| H["⚡ Execute Switch"]
    G -->|"❌ Reject"| I["🛑 Workflow Ends"]
    
    H --> J["✅ Done"]

    style F fill:#FF6B35,stroke:#333,color:#fff,font-weight:bold
    style G fill:#4ECDC4,stroke:#333,color:#fff,font-weight:bold
    style D fill:#7B68EE,stroke:#333,color:#fff
```
