type LastUpdatedProps = {
  date: string;
};

export function LastUpdated({ date }: LastUpdatedProps) {
  return (
    <div className="inline-flex items-center rounded-full border border-border/70 bg-muted/70 px-3 py-1 text-xs font-medium text-muted-foreground">
      Son güncelleme: {date}
    </div>
  );
}

export default LastUpdated;
