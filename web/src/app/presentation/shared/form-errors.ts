import { AbstractControl, FormGroup } from '@angular/forms';
import { ValidationError } from '../../domain/errors';

/** Human-readable message for the first error on a touched control, or null. */
export function fieldError(control: AbstractControl | null): string | null {
  if (!control || !control.invalid || !(control.touched || control.dirty)) {
    return null;
  }

  const errors = control.errors ?? {};
  if (errors['server']) return errors['server'] as string;
  if (errors['required']) return 'This field is required.';
  if (errors['min']) return `Must be at least ${errors['min'].min}.`;
  if (errors['max']) return `Must be at most ${errors['max'].max}.`;
  if (errors['maxlength'])
    return `Must be at most ${errors['maxlength'].requiredLength} characters.`;
  return 'Invalid value.';
}

/**
 * Attaches API validation messages to the matching form controls so they show inline.
 * Returns messages for fields that are not on the form (e.g. catalog fields).
 */
export function applyServerErrors(form: FormGroup, error: ValidationError): string[] {
  const unmatched: string[] = [];
  for (const [field, messages] of Object.entries(error.fieldErrors)) {
    const control = form.get(field);
    if (control) {
      control.setErrors({ server: messages[0] });
      control.markAsTouched();
    } else {
      unmatched.push(...messages);
    }
  }
  return unmatched;
}
