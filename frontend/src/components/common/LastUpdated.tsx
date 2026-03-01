type LastUpdatedProps = {
  date: string;
};

export function LastUpdated({ date }: LastUpdatedProps) {
  return (
    <div className="inline-flex items-center rounded-full border border-white/10 bg-white/5 px-3 py-1 text-xs font-medium text-gray-300">
      Son güncelleme: {date}
    </div>
  );
}

export default LastUpdated;
