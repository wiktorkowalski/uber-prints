export const Footer = () => {
  return (
    <footer className="border-t bg-background mt-auto">
      <div className="container mx-auto px-4 py-6">
        <div className="flex flex-col md:flex-row justify-between items-center space-y-4 md:space-y-0">
          <div className="text-sm text-muted-foreground">
            © {new Date().getFullYear()} UberPrints. All rights reserved.
          </div>

          <div className="flex items-center space-x-4 text-sm text-muted-foreground">
            <span>3D Printing Request System</span>
          </div>
        </div>
      </div>
    </footer>
  );
};
