export type DashboardPeriod = 'daily' | 'weekly' | 'monthly';

export interface ChartTimePoint {
  label: string;
  date: string;
}

export interface RevenueTrendChartPoint extends ChartTimePoint {
  revenue: number;
  orders: number;
}

export interface ComparativeRevenueTrendChartPoint extends RevenueTrendChartPoint {
  previousRevenue: number;
}

export interface RegistrationChartPoint extends ChartTimePoint {
  count: number;
}

export interface StatusDistributionChartPoint<TStatus extends string = string> {
  status: TStatus;
  count: number;
}
