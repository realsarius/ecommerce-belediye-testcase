import json
import random
from datetime import datetime, timedelta

inventories = []

for product_id in range(1, 201):
    days_ago = random.randint(1, 30)
    updated = datetime.now() - timedelta(days=days_ago)
    
    quantity_available = random.randint(0, 500)
    quantity_reserved = random.randint(0, min(50, quantity_available))  # Reserved <= Available
    
    inventories.append({
        "productId": product_id,
        "quantityAvailable": quantity_available,
        "quantityReserved": quantity_reserved,
        "updatedAt": updated.strftime("%Y-%m-%dT%H:%M:%SZ")
    })

with open("seed-data/inventories.json", "w", encoding="utf-8") as f:
    json.dump(inventories, f, ensure_ascii=False, indent=2)

print(f"✅ {len(inventories)} stok kaydı oluşturuldu!")
print(f"📁 Dosya: seed-data/inventories.json")
