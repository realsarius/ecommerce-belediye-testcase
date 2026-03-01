import { StaticPageLayout } from '@/components/common/StaticPageLayout';

const pricingRows = [
  ['Elektronik', '%12', 'Yüksek işlem güvenliği ve iade takibi'],
  ['Moda', '%15', 'Kampanya ve görünürlük desteği'],
  ['Ev & Yaşam', '%14', 'Kategori vitrini ve kampanya katılımı'],
  ['Kitap', '%10', 'Temel mağaza operasyon desteği'],
];

export default function SellerPricing() {
  return (
    <StaticPageLayout
      eyebrow="Satıcı"
      title="Komisyon Oranları"
      description="Kategori bazlı örnek komisyon oranları ve satıcı hizmet kapsamı hakkında özet tablo."
      lastUpdated="Mart 2026"
    >
      <div className="overflow-hidden rounded-3xl border border-white/10 bg-white/[0.03]">
        <table className="w-full text-left text-sm">
          <thead className="bg-white/5 text-gray-200">
            <tr>
              <th className="px-4 py-3 font-medium">Kategori</th>
              <th className="px-4 py-3 font-medium">Komisyon</th>
              <th className="px-4 py-3 font-medium">Not</th>
            </tr>
          </thead>
          <tbody>
            {pricingRows.map((row) => (
              <tr key={row[0]} className="border-t border-white/10">
                <td className="px-4 py-3">{row[0]}</td>
                <td className="px-4 py-3">{row[1]}</td>
                <td className="px-4 py-3 text-gray-400">{row[2]}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </StaticPageLayout>
  );
}
