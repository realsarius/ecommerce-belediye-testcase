export type CreateContactMessageRequest = {
  name: string;
  email: string;
  subject: string;
  message: string;
};

export type ContactMessage = {
  id: number;
  name: string;
  email: string;
  subject: string;
  message: string;
  createdAt: string;
};
